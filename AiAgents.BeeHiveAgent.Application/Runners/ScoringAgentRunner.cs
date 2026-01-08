using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Application.Services;
using AiAgents.BeeHiveAgent.Domain.Entities;
using AiAgents.BeeHiveAgent.Domain.Enums;
using AiAgents.Core.Abstractions;
using Microsoft.EntityFrameworkCore; // <--- OVO NEDOSTAJE
namespace AiAgents.BeeHiveAgent.Application.Runners;

// Definiramo rezultat jednog Tick-a (za logove i UI)
public record ScoringTickResult(Guid SampleId, string OldStatus, string NewStatus, float Score, string Decision) : IResult;
public class ScoringAgentRunner : SoftwareAgent<HiveImageSample, Prediction, ScoringTickResult>
{
    private readonly IAppDbContext _db;
    private readonly IBeeImageClassifier _classifier;
    private readonly ScoringPolicy _policy;

    public ScoringAgentRunner(IAppDbContext db, IBeeImageClassifier classifier, ScoringPolicy policy)
    {
        _db = db;
        _classifier = classifier;
        _policy = policy;
    }

    public override async Task<ScoringTickResult?> StepAsync(CancellationToken ct)
    {
        // 1. SENSE: Nađi jedan sample koji čeka (Queued)
        // Koristimo jednostavan lock mehanizam promjenom statusa
        var sample = await _db.ImageSamples
            .Where(s => s.Status == SampleStatus.Queued)
            .OrderBy(s => s.CapturedAt)
            .FirstOrDefaultAsync(ct);

        if (sample == null) return null; // NoWork

        // Odmah zaključaj da drugi worker ne uzme isto (Idempotencija)
        sample.MarkProcessing();
        await _db.SaveChangesAsync(ct);

        // 2. THINK: Pozovi ML model
        // Pretpostavljamo da classifier vraća vjerovatnoću za "Target Label" (npr. Pollen)
        var predictions = await _classifier.PredictAsync(sample.ImagePath);

        // Uzmi najvjerovatniju klasu
        var bestMatch = predictions.OrderByDescending(k => k.Value).First();
        var score = bestMatch.Value;
        var label = bestMatch.Key;

        // Učitaj postavke za pravila
        var settings = await _db.Settings.FirstOrDefaultAsync(ct)
                       ?? new SystemSettings(); // Fallback ako nema settingsa

        // 3. ACT: Primijeni Policy i spasi
        var decisionResult = _policy.Evaluate(score, label, settings);

        // Kreiraj predikciju
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            SampleId = sample.Id,
            Score = score,
            PredictedLabel = label,
            Decision = decisionResult.Decision,
            CreatedAt = DateTime.UtcNow,
            // ModelVersionId = settings.ActiveModelVersionId // TODO: Dodati kad implementiramo verzije
        };

        // Ažuriraj entitete
        _db.Predictions.Add(prediction);
        sample.Status = decisionResult.NewStatus;

        await _db.SaveChangesAsync(ct);

        return new ScoringTickResult(sample.Id, "Queued", sample.Status.ToString(), score, decisionResult.Decision.ToString());
    }
}