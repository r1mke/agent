using AiAgents.BeeHiveAgent.Application.Runners;

namespace AiAgents.BeeHiveAgent.Web.Workers;

/// <summary>
/// Background worker koji periodično pokreće ScoringAgentRunner.
/// Ovaj "host" je odgovoran za procesiranje novih slika u redu čekanja.
/// </summary>
public class ScoringWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScoringWorker> _logger;

    public ScoringWorker(IServiceProvider serviceProvider, ILogger<ScoringWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("═══════════════════════════════════════════════════");
        _logger.LogInformation("🐝 SCORING WORKER: Pokrenut!");
        _logger.LogInformation("═══════════════════════════════════════════════════");

        // Čekaj malo da se aplikacija potpuno pokrene
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var runner = scope.ServiceProvider.GetRequiredService<ScoringAgentRunner>();

                    // Pokreni ciklus agenta (Sense-Think-Act)
                    var result = await runner.StepAsync(stoppingToken);

                    if (result != null)
                    {
                        // Slika je procesirana
                        _logger.LogInformation($"✅ SCORED: ID={result.SampleId}");
                        _logger.LogInformation($"   -> Score: {result.Score:P1}");
                        _logger.LogInformation($"   -> Decision: {result.Decision}");
                        _logger.LogInformation($"   -> Status: {result.OldStatus} -> {result.NewStatus}");

                        // Kratka pauza između slika
                        await Task.Delay(100, stoppingToken);
                    }
                    else
                    {
                        // Nema slika za procesiranje - čekaj 2 sekunde
                        await Task.Delay(2000, stoppingToken);
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
                _logger.LogError(ex, "❌ SCORING WORKER: Greška u petlji!");

                // Nakon greške, čekaj prije ponovnog pokušaja
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("🛑 SCORING WORKER: Zaustavljen.");
    }
}