using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Domain.Entities;
using Microsoft.ML;

namespace AiAgents.BeeHiveAgent.Infrastructure.ML;

/// <summary>
/// Servis za treniranje ML modela koristeći ML.NET Image Classification.
/// Implementira IModelTrainer interface za dependency injection.
/// </summary>
public class TrainingService : IModelTrainer
{
    private readonly string _modelsFolder;
    private readonly string _modelPath;
    private readonly MLContext _mlContext;

    // Event koji se okida kada je novi model spreman (za reload classifiera)
    public static event Action? OnModelTrained;

    public TrainingService()
    {
        _mlContext = new MLContext(seed: 42);

        // Koristimo trenutni direktorij
        _modelsFolder = Path.Combine(Directory.GetCurrentDirectory(), "MLModels");
        _modelPath = Path.Combine(_modelsFolder, "bee_model.zip");

        if (!Directory.Exists(_modelsFolder))
            Directory.CreateDirectory(_modelsFolder);
    }

    public bool ModelExists()
    {
        return File.Exists(_modelPath);
    }

    public string TrainModel(List<HiveImageSample> goldSamples)
    {
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine("🐝 POKREĆEM TRENING ML MODELA");
        Console.WriteLine("═══════════════════════════════════════════════════");

        // 1. VALIDACIJA PODATAKA
        var countPollen = goldSamples.Count(s => s.Label == "Pollen");
        var countNoPollen = goldSamples.Count(s => s.Label == "NoPollen");

        Console.WriteLine($"📊 Dataset statistika:");
        Console.WriteLine($"   - Pollen slike: {countPollen}");
        Console.WriteLine($"   - NoPollen slike: {countNoPollen}");
        Console.WriteLine($"   - Ukupno: {goldSamples.Count}");

        if (countPollen == 0 || countNoPollen == 0)
        {
            Console.WriteLine("❌ GREŠKA: Fali jedna od klasa! Trening nije moguć.");
            return "SKIPPED_BAD_DATA";
        }

        // Provjeri da slike fizički postoje
        var validSamples = goldSamples.Where(s => File.Exists(s.ImagePath)).ToList();
        Console.WriteLine($"✅ Validnih slika (postoje na disku): {validSamples.Count}");

        if (validSamples.Count < 10)
        {
            Console.WriteLine("❌ GREŠKA: Premalo validnih slika za trening!");
            return "SKIPPED_BAD_DATA";
        }

        // 2. PRIPREMA PODATAKA
        var trainData = validSamples.Select(s => new ModelInput
        {
            ImagePath = s.ImagePath,
            Label = s.Label ?? "NoPollen"
        }).ToList();

        var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainData);

        // 3. KREIRANJE PIPELINE-A
        Console.WriteLine("🔧 Kreiram ML pipeline...");

        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                inputColumnName: "Label",
                outputColumnName: "LabelKey")
            .Append(_mlContext.Transforms.LoadRawImageBytes(
                outputColumnName: "Image",
                imageFolder: null,
                inputColumnName: "ImagePath"))
            .Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(
                featureColumnName: "Image",
                labelColumnName: "LabelKey"))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                outputColumnName: "PredictedLabel",
                inputColumnName: "PredictedLabel"));

        // 4. TRENING
        Console.WriteLine("💪 Započinjem trening modela (ovo može potrajati)...");
        var startTime = DateTime.Now;

        ITransformer trainedModel;
        try
        {
            trainedModel = pipeline.Fit(trainingDataView);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška tokom treninga: {ex.Message}");
            return "TRAINING_FAILED";
        }

        var duration = DateTime.Now - startTime;
        Console.WriteLine($"⏱️ Trening završen za: {duration.TotalSeconds:F1} sekundi");

        // 5. SPAŠAVANJE MODELA
        Console.WriteLine($"💾 Spašavam model na: {_modelPath}");

        try
        {
            _mlContext.Model.Save(trainedModel, trainingDataView.Schema, _modelPath);
            Console.WriteLine("✅ Model uspješno spašen!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Greška pri spašavanju: {ex.Message}");
            return "SAVE_FAILED";
        }

        // 6. OBAVIJESTI CLASSIFIER DA RELOAD-A MODEL
        OnModelTrained?.Invoke();

        var version = $"v{DateTime.Now:yyyyMMdd_HHmmss}";
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine($"🎉 TRENING KOMPLETIRAN! Nova verzija: {version}");
        Console.WriteLine("═══════════════════════════════════════════════════");

        return version;
    }
}