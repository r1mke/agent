using Microsoft.ML.Data;

namespace AiAgents.BeeHiveAgent.Infrastructure.ML;

/// <summary>
/// Ulazna klasa za ML model - mapira putanju slike i labelu.
/// </summary>
public class ModelInput
{
    /// <summary>
    /// Puna putanja do slike na disku.
    /// </summary>
    [LoadColumn(0)]
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Labela klase: "Pollen" ili "NoPollen"
    /// </summary>
    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Izlazna klasa ML modela - rezultat predikcije.
/// </summary>
public class ModelOutput
{
    /// <summary>
    /// Predviđena klasa ("Pollen" ili "NoPollen")
    /// </summary>
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    /// <summary>
    /// Niz vjerovatnoća za svaku klasu.
    /// Index 0 može biti "NoPollen", Index 1 "Pollen" (zavisi od treninga)
    /// </summary>
    [ColumnName("Score")]
    public float[] Score { get; set; } = Array.Empty<float>();
}