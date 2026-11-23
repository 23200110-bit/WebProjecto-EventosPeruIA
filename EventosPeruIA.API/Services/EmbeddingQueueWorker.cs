using EventosPeruIA.CORE.Core.Interfaces;

namespace EventosPeruIA.API.Services;

public sealed class EmbeddingQueueWorker(IServiceScopeFactory scopeFactory, ILogger<EmbeddingQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var embeddings = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
                await embeddings.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process pending RAG document embeddings.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
