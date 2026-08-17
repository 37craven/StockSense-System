using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;
using System.Security.Claims;
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
using StockSense.Web.Services;
using StockSense.Web.Utility.Security;
using StockSense.Application.Interfaces;
using StockSense.Web.Options;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CORE SERVICES ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddLocalization();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("InventoryStaff", policy => policy.RequireRole("Admin", "Employee"))
    .AddPolicy("InventoryAdministrator", policy => policy.RequireRole("Admin"));

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
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = 403;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// --- 3. DATABASE ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=StockSense;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true";

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
    options.AddPolicy("api-policy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } userId
                ? $"user:{userId}"
                : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 10,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

// --- 7. ADDITIONAL SERVICES ---
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// --- CONCRETE REPOSITORIES ---
builder.Services.AddScoped<PreBuiltRepository>();
builder.Services.AddScoped<MotorcycleRepository>();
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
builder.Services.AddScoped<IOrderEmailSender, OrderEmailSender>();
builder.Services.AddSingleton<PdfDownloadCache>();
builder.Services.AddScoped<ISafetyStockCalculationService, SafetyStockCalculationService>();
builder.Services.AddScoped<IOrderSlipWorkflowService, OrderSlipWorkflowService>();
builder.Services.AddScoped<IWorkOrderCheckoutService, WorkOrderCheckoutService>();
builder.Services.AddScoped<IAdminPinService, AdminPinService>();
builder.Services.AddScoped<IMotorCompatibilityLookupService, MotorCompatibilityLookupService>();
builder.Services.AddScoped<IBuildWorkOrderMutationService, StockSense.Web.Services.BuildWorkOrderMutationService>();

// --- HELPERS (concrete, no interfaces) ---
builder.Services.AddScoped<OrderSlipHelper>();
builder.Services.AddScoped<TransactionHelper>();

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddBlazorBlueprintPrimitives();
// ponytail: unconfigured HttpClient for prerendered layout components (PublicNav, NavBar, NavMenu)
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
// ponytail: server-rendered components (e.g. AssistanceChat) call cookie-authenticated APIs.
// This client forwards the browser cookies so server islands match WASM islands.
// The handler only ever rewrites RELATIVE urls to the app's own origin (self-requests),
// so the relaxed cert callback below never applies to third-party hosts.
builder.Services.AddScoped(sp =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    var inner = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };
    var client = new HttpClient(
        new StockSense.Web.Helpers.CookieForwardingHandler(accessor, inner));
    var context = accessor.HttpContext;
    if (context is not null)
        client.BaseAddress = new Uri($"{context.Request.Scheme}://{context.Request.Host}");
    return client;
});
builder.Services.AddOptions<ChatbotOptions>()
    .Bind(builder.Configuration.GetSection(ChatbotOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => options.BaseUrl.IsAbsoluteUri, "Chatbot:BaseUrl must be an absolute URL.")
    .Validate(options => options.HasSupportedScheme(), "Chatbot:BaseUrl must use HTTP or HTTPS.")
    .Validate(
        options => string.IsNullOrEmpty(options.BaseUrl.Query) && string.IsNullOrEmpty(options.BaseUrl.Fragment),
        "Chatbot:BaseUrl cannot contain a query string or fragment.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IAssistanceClient, AssistanceClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatbotOptions>>().Value;
    client.BaseAddress = options.BaseUrl;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new PhDateTimeConverter());
    });

var app = builder.Build();
var entryAssembly = Assembly.GetEntryAssembly() ?? typeof(Program).Assembly;
var assemblyName = entryAssembly.GetName();
var binaryFingerprint = new
{
    Name = assemblyName.Name ?? "unknown",
    Version = assemblyName.Version?.ToString() ?? "unknown",
    InformationalVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
    ModuleVersionId = entryAssembly.ManifestModule.ModuleVersionId
};
app.Logger.LogInformation(
    "Running binary: {AssemblyName} {AssemblyVersion}; informationalVersion={InformationalVersion}; moduleVersionId={ModuleVersionId}",
    binaryFingerprint.Name,
    binaryFingerprint.Version,
    binaryFingerprint.InformationalVersion,
    binaryFingerprint.ModuleVersionId);

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
    try
    {
        await next(context);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // The browser closed, refreshed, or navigated away. The response transport is
        // already gone, so attempting to serialize an API error only creates a second
        // teardown exception in the debugger.
    }
    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/api"))
    {
        if (context.Response.HasStarted)
            throw;

        app.Logger.LogError(ex, "Unhandled API request failure for {RequestPath}.", context.Request.Path);
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

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Admin", "Employee", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var usersWithEmptyRole = await userManager.Users
            .Where(u => string.IsNullOrEmpty(u.Role))
            .ToListAsync();
        foreach (var user in usersWithEmptyRole)
        {
            var identityRoles = await userManager.GetRolesAsync(user);
            if (identityRoles.Count == 1)
            {
                user.Role = identityRoles[0];
                await userManager.UpdateAsync(user);
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed. Application startup is stopping.");
        throw;
    }
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isAuthorizationAuditRoute = path.StartsWithSegments("/api/products")
        || path.StartsWithSegments("/api/inventory");
    if (!isAuthorizationAuditRoute)
    {
        await next(context);
        return;
    }

    var endpoint = context.GetEndpoint();
    var authorizeData = endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>() ?? [];
    var policyProvider = context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
    var policy = authorizeData.Count == 0
        ? null
        : await AuthorizationPolicy.CombineAsync(policyProvider, authorizeData);
    var email = context.User.FindFirstValue(ClaimTypes.Email)
        ?? context.User.FindFirstValue(ClaimTypes.Name)
        ?? "anonymous";
    var roles = context.User.FindAll(ClaimTypes.Role)
        .Select(claim => claim.Value)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    var policyNames = authorizeData
        .Select(data => data.Policy)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToArray();
    var allowedRoles = authorizeData
        .SelectMany(data => (data.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    var requirements = policy?.Requirements
        .Select(requirement => requirement.GetType().Name)
        .Distinct(StringComparer.Ordinal)
        .ToArray() ?? [];

    try
    {
        await next(context);
    }
    finally
    {
        app.Logger.LogInformation(
            "Authorization audit: {Method} {Path} => {StatusCode}; authenticated={IsAuthenticated}; email={Email}; roles={Roles}; endpoint={Endpoint}; policies={Policies}; allowedRoles={AllowedRoles}; requirements={Requirements}; binary={AssemblyName}/{AssemblyVersion}/{InformationalVersion}/{ModuleVersionId}",
            context.Request.Method,
            path.Value,
            context.Response.StatusCode,
            context.User.Identity?.IsAuthenticated == true,
            email,
            roles.Length == 0 ? "(none)" : string.Join(',', roles),
            endpoint?.DisplayName ?? "(unmatched)",
            policyNames.Length == 0 ? "(default/inline)" : string.Join(',', policyNames),
            allowedRoles.Length == 0 ? "(none)" : string.Join(',', allowedRoles),
            requirements.Length == 0 ? "(none)" : string.Join(',', requirements),
            binaryFingerprint.Name,
            binaryFingerprint.Version,
            binaryFingerprint.InformationalVersion,
            binaryFingerprint.ModuleVersionId);
    }
});
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StockSense.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();
app.MapControllers().RequireRateLimiting("api-policy");

app.MapGet("/api/download/{token}", (string token, PdfDownloadCache cache) =>
{
    var data = cache.Retrieve(token);
    return data is null ? Results.NotFound("Download expired or not found.") : Results.File(data, "application/pdf");
});

app.Run();
