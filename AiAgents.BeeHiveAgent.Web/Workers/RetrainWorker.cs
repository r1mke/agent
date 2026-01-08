using AiAgents.BeeHiveAgent.Application.Runners;

namespace AiAgents.BeeHiveAgent.Web.Workers;

/// <summary>
/// Background worker koji periodično pokreće RetrainAgentRunner.
/// Ovaj "host" je odgovoran za životni ciklus agenta za učenje.
/// </summary>
public class RetrainWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetrainWorker> _logger;

    public RetrainWorker(IServiceProvider serviceProvider, ILogger<RetrainWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("🎓 RETRAIN WORKER: Pokrenut!");
        _logger.LogInformation("═══════════════════════════════════════════════════");

        // Čekaj malo da se aplikacija potpuno pokrene
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var runner = scope.ServiceProvider.GetRequiredService<RetrainAgentRunner>();

                    _logger.LogDebug("🔍 RETRAIN WORKER: Provjeravam da li ima posla...");

                    // Pokreni ciklus agenta (Sense-Think-Act)
                    var result = await runner.StepAsync(stoppingToken);

                    if (result != null)
                    {
                        // Trening je završen!
                        _logger.LogInformation("═══════════════════════════════════════════════════");
                        _logger.LogInformation($"🎉 RETRAIN COMPLETE!");
                        _logger.LogInformation($"   -> Nova verzija: {result.NewModelVersion}");
                        _logger.LogInformation($"   -> Trenirano na: {result.TrainingCount} slika");
                        _logger.LogInformation("═══════════════════════════════════════════════════");

                        // Nakon treninga, čekaj duže prije sljedeće provjere
                        await Task.Delay(60000, stoppingToken); // 1 minuta
                    }
                    else
                    {
                        // Nema posla - čekaj 10 sekundi pa provjeri ponovo
                        await Task.Delay(10000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normalno gašenje
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RETRAIN WORKER: Greška u petlji!");

                // Nakon greške, čekaj duže prije ponovnog pokušaja
                await Task.Delay(30000, stoppingToken);
            }
        }

        _logger.LogInformation("🛑 RETRAIN WORKER: Zaustavljen.");
    }
}