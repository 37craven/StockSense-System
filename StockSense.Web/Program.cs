using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using BlazorBlueprint.Components;
using BlazorBlueprint.Primitives;
using BlazorBlueprint.Primitives.Extensions;
using StockSense.Web.Components;
using StockSense.Web.Components.Account;
using StockSense.Web.Helpers;
using StockSense.Infrastructure.Data;
using StockSense.Infrastructure.Services;
using StockSense.Infrastructure.Data.Repositories;
using StockSense.Web.Utility.Security;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORE SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddLocalization();

// --- 2. AUTHENTICATION & COOKIES ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = 401;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// --- 3. DATABASE ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 4. IDENTITY ---
builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.AllowedForNewUsers = true;
});

// --- 5. EMAIL ---
builder.Services.AddTransient<StockSense.Application.Interfaces.IEmailSender<ApplicationUser>, EmailSender>();
builder.Services.AddTransient<EmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<ApplicationUser>, IdentityEmailAdapter>();

// --- 6. RATE LIMITING ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(policyName: "login-policy", opt =>
    {
        opt.PermitLimit = 5; opt.Window = TimeSpan.FromSeconds(30); opt.QueueLimit = 0;
    });
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            { AutoReplenishment = true, PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});

// --- 7. ADDITIONAL SERVICES ---
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// --- CONCRETE REPOSITORIES ---
builder.Services.AddScoped<PreBuiltRepository>();
builder.Services.AddScoped<OrderSlipRepository>();
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<AppointmentRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<PinnedSlipRepository>();
builder.Services.AddScoped<MechanicRepository>();
builder.Services.AddScoped<BuildRequestRepository>();
builder.Services.AddScoped<StoreServiceRepository>();

// --- INFRASTRUCTURE (concrete) ---
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<BarcodeService>();
builder.Services.AddScoped<OrderEmailSender>();
builder.Services.AddSingleton<PdfDownloadCache>();

// --- HELPERS (concrete, no interfaces) ---
builder.Services.AddScoped<OrderSlipHelper>();
builder.Services.AddScoped<TransactionHelper>();

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddBlazorBlueprintPrimitives();
// ponytail: unconfigured HttpClient for prerendered layout components (PublicNav, NavBar, NavMenu)
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new PhDateTimeConverter());
    });

var app = builder.Build();

// --- 8. PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    }
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (context.Database.CanConnect() && context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex) { Console.WriteLine("STARTUP ERROR (Migration): " + ex.Message); }
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StockSense.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();
app.MapControllers();

app.MapGet("/api/download/{token}", (string token, PdfDownloadCache cache) =>
{
    var data = cache.Retrieve(token);
    return data is null ? Results.NotFound("Download expired or not found.") : Results.File(data, "application/pdf");
});

app.Run();
