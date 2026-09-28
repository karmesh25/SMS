using ABR.Application.Interfaces;

namespace ABR.Api.HostedServices;

/// <summary>
/// Daily job: capitalizes unpaid completed months of vyaj into principal.
/// </summary>
public sealed class VyajCapitalizationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VyajCapitalizationHostedService> _logger;
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public VyajCapitalizationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VyajCapitalizationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var vyaj = scope.ServiceProvider.GetRequiredService<IVyajService>();
                var created = await vyaj.CapitalizeUnpaidMonthsAsync(stoppingToken);
                if (created > 0)
                    _logger.LogInformation("Vyaj capitalization created {Count} payment(s).", created);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vyaj month-end capitalization failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
