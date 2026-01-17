using Microsoft.ML.Data;

namespace AiAgents.BeeHiveAgent.Infrastructure.ML;


public class ModelInput
{

    [LoadColumn(0)]
    public string ImagePath { get; set; } = string.Empty;


    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}


public class ModelOutput
{

    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;


    [ColumnName("Score")]
    public float[] Score { get; set; } = Array.Empty<float>();
}