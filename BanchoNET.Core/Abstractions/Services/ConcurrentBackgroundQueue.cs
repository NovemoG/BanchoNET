using Microsoft.Extensions.Hosting;

namespace BanchoNET.Core.Abstractions.Services;

public abstract class ConcurrentBackgroundQueue : BackgroundService
{
    private const int MAX_CONCURRENT_JOBS = 5;
    
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken
    ) {
        var workers = Enumerable.Range(0, MAX_CONCURRENT_JOBS)
            .Select(_ => WorkerLoop(stoppingToken));
        
        await Task.WhenAll(workers);
    }
    
    protected abstract Task WorkerLoop(CancellationToken stoppingToken);
}