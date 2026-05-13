namespace OAI.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OAI.Worker is running.");

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
