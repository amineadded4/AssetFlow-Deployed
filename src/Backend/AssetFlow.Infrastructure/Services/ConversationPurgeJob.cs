// src/Backend/AssetFlow.Infrastructure/Services/ConversationPurgeJob.cs
using AssetFlow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetFlow.Infrastructure.Services
{
    /// <summary>
    /// Service hébergé qui tourne en arrière-plan et purge chaque jour
    /// les conversations Redis de plus de 30 jours.
    /// </summary>
    public class ConversationPurgeJob : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ConversationPurgeJob> _logger;

        // Intervalle de vérification : toutes les 24 h
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        public ConversationPurgeJob(IServiceProvider services, ILogger<ConversationPurgeJob> logger)
        {
            _services = services;
            _logger   = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConversationPurgeJob démarré.");

            // Première purge au démarrage de l'application
            await RunPurgeAsync();

            using var timer = new PeriodicTimer(Interval);

            while (!stoppingToken.IsCancellationRequested &&
                   await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunPurgeAsync();
            }
        }

        private async Task RunPurgeAsync()
        {
            try
            {
                using var scope = _services.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IConversationHistoryService>();
                await svc.PurgeExpiredConversationsAsync();
                _logger.LogInformation("[{Time}] Purge des conversations expirées effectuée.", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la purge des conversations expirées.");
            }
        }
    }
}