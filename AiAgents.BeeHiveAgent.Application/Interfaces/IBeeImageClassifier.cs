namespace AiAgents.BeeHiveAgent.Application.Interfaces;

public interface IBeeImageClassifier
{
    // Vraća dictionary: "Pollen" -> 0.95, "NoPollen" -> 0.05
    Task<Dictionary<string, float>> PredictAsync(string imagePath);
}
