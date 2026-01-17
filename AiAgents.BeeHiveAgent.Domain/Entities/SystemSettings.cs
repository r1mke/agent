using AiAgents.Core.Abstractions;

namespace AiAgents.BeeHiveAgent.Domain.Entities;


public class SystemSettings : IPercept
{
    public int Id { get; set; }


    public int NewGoldSinceLastTrain { get; set; }


    public int RetrainGoldThreshold { get; set; } = 50;
    public float AutoThresholdHigh { get; set; } = 0.90f;
    public float AutoThresholdLow { get; set; } = 0.15f;

    public bool IsRetrainEnabled { get; set; } = true;
    public Guid? ActiveModelVersionId { get; set; }
}