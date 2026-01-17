using AiAgents.BeeHiveAgent.Application.Interfaces;
using AiAgents.BeeHiveAgent.Infrastructure.ML;
using Microsoft.ML;

namespace AiAgents.BeeHiveAgent.Infrastructure.Services;


public class MLNetBeeClassifier : IBeeImageClassifier, IDisposable
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _trainedModel;
    private PredictionEngine<ModelInput, ModelOutput>? _predictionEngine;
    private readonly object _lock = new object();
    private bool _disposed = false;

    public MLNetBeeClassifier()
    {
        _mlContext = new MLContext();
        _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "bee_model.zip");

        TrainingService.OnModelTrained += ReloadModel;


        LoadModel();
    }

    private void LoadModel()
    {
        lock (_lock)
        {
            if (!File.Exists(_modelPath))
            {
                Console.WriteLine($"⚠️ MLNetBeeClassifier: Model nije nađen na {_modelPath}");
                Console.WriteLine("   -> Čekam da RetrainAgent završi prvi trening...");
                _predictionEngine = null;
                return;
            }

            try
            {
                Console.WriteLine($"📂 MLNetBeeClassifier: Učitavam model iz {_modelPath}");

                DataViewSchema modelSchema;
                _trainedModel = _mlContext.Model.Load(_modelPath, out modelSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_trainedModel);

                Console.WriteLine("✅ MLNetBeeClassifier: Model uspješno učitan i spreman za predikcije!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MLNetBeeClassifier: Greška pri učitavanju modela: {ex.Message}");
                _predictionEngine = null;
            }
        }
    }


    private void ReloadModel()
    {
        Console.WriteLine("🔄 MLNetBeeClassifier: Primljena obavijest o novom modelu, reload u toku...");
        LoadModel();
    }

    public Task<Dictionary<string, float>> PredictAsync(string imagePath)
    {
        var result = new Dictionary<string, float>();


        if (_predictionEngine == null)
        {

            LoadModel();

            if (_predictionEngine == null)
            {
                Console.WriteLine($"⚠️ PREDIKCIJA PRESKOČENA: Model još nije istreniran!");
                Console.WriteLine($"   -> Slika: {Path.GetFileName(imagePath)}");
                Console.WriteLine($"   -> Čekam da se nakupi dovoljno gold podataka za trening...");


                result.Add("Unknown", 0.0f);
                return Task.FromResult(result);
            }
        }


        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"❌ PREDIKCIJA GREŠKA: Slika ne postoji: {imagePath}");
            result.Add("Error_FileNotFound", 0.0f);
            return Task.FromResult(result);
        }


        try
        {
            var input = new ModelInput { ImagePath = imagePath };

            ModelOutput prediction;
            lock (_lock)
            {
                prediction = _predictionEngine.Predict(input);
            }


            if (prediction.Score != null && prediction.Score.Length > 0)
            {
                float maxScore = prediction.Score.Max();
                string predictedLabel = prediction.PredictedLabel ?? "Unknown";

                result.Add(predictedLabel, maxScore);


                Console.WriteLine($"🧠 PREDIKCIJA: {Path.GetFileName(imagePath)}");
                Console.WriteLine($"   -> Label: {predictedLabel}");
                Console.WriteLine($"   -> Confidence: {maxScore:P1}");
                Console.WriteLine($"   -> All scores: [{string.Join(", ", prediction.Score.Select(s => s.ToString("F3")))}]");
            }
            else
            {
                Console.WriteLine($"⚠️ PREDIKCIJA: Prazan rezultat za {Path.GetFileName(imagePath)}");
                result.Add("Unknown", 0.0f);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PREDIKCIJA GREŠKA: {ex.Message}");
            result.Add("Error", 0.0f);
        }

        return Task.FromResult(result);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            TrainingService.OnModelTrained -= ReloadModel;
            _predictionEngine?.Dispose();
            _disposed = true;
        }
    }
}