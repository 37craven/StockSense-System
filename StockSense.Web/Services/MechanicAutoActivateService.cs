using StockSense.Infrastructure.Data.Repositories;

namespace StockSense.Web.Services;

public class MechanicAutoActivateService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MechanicAutoActivateService> _logger;

    public MechanicAutoActivateService(IServiceProvider serviceProvider, ILogger<MechanicAutoActivateService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check every hour; on startup also check after 10s
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<MechanicRepository>();
                var count = await repo.AutoActivateExpiredAsync();
                if (count > 0) _logger.LogInformation("Auto-activated {Count} mechanics after inactive period ended.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-activate mechanics.");
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
