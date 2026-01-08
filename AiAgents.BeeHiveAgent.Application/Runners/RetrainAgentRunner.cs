using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Domain.Entities;
using AiAgents.BeeHiveAgent.Domain.Enums;
using AiAgents.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AiAgents.BeeHiveAgent.Application.Runners;

/// <summary>
/// Rezultat Learn ciklusa - informacije o završenom treningu.
/// </summary>
public record RetrainResult(string NewModelVersion, int TrainingCount) : IResult;

/// <summary>
/// Agent odgovoran za automatski re-trening ML modela kada se nakupi
/// dovoljno novih "gold" podataka (ljudski pregledanih slika).
/// 
/// Ciklus: SENSE (provjeri settings) -> THINK (ima li dovoljno podataka?) -> ACT (treniraj!)
/// </summary>
public class RetrainAgentRunner : SoftwareAgent<SystemSettings, Prediction, RetrainResult>
{
    private readonly IAppDbContext _db;
    private readonly IModelTrainer _trainer;

    public RetrainAgentRunner(IAppDbContext db, IModelTrainer trainer)
    {
        _db = db;
        _trainer = trainer;
    }

    public override async Task<RetrainResult?> StepAsync(CancellationToken ct)
    {
        // 1. SENSE: Provjeri settings
        var settings = await _db.Settings.FirstOrDefaultAsync(ct);
        if (settings == null || !settings.IsRetrainEnabled)
        {
            return null;
        }

        // 2. THINK: Da li imamo dovoljno novih gold podataka?
        if (settings.NewGoldSinceLastTrain < settings.RetrainGoldThreshold)
        {
            // Nema dovoljno podataka za trening, spavaj
            return null;
        }

        Console.WriteLine($"🎓 RETRAIN AGENT: Pokrećem trening! Gold podataka: {settings.NewGoldSinceLastTrain}");

        // 3. ACT: Pokreni trening!
        // Učitaj sve Gold slike iz baze (status = Reviewed)
        var goldSamples = await _db.ImageSamples
            .Where(s => s.Status == SampleStatus.Reviewed && !string.IsNullOrEmpty(s.Label))
            .ToListAsync(ct);

        if (goldSamples.Count == 0)
        {
            Console.WriteLine("⚠️ RETRAIN AGENT: Nema gold samplea sa labelama!");
            return null;
        }

        // Pozovi trenera
        var newModelVersion = _trainer.TrainModel(goldSamples);

        // Ako je trening preskočen zbog loših podataka
        if (newModelVersion == "SKIPPED_BAD_DATA")
        {
            Console.WriteLine("⚠️ RETRAIN AGENT: Trening preskočen - nedostaje jedna od klasa!");
            return null;
        }

        // Ažuriraj sistem - resetuj brojač
        settings.NewGoldSinceLastTrain = 0;

        await _db.SaveChangesAsync(ct);

        Console.WriteLine($"✅ RETRAIN AGENT: Trening završen! Nova verzija: {newModelVersion}");

        return new RetrainResult(newModelVersion, goldSamples.Count);
    }
}