using AiAgents.Core.Abstractions; // <--- OVO JE NEDOSTAJALO

namespace AiAgents.BeeHiveAgent.Domain.Entities;

// Dodajemo : IPercept da bi ga RetrainAgentRunner mogao koristiti
public class SystemSettings : IPercept
{
    public int Id { get; set; }

    // Counters
    public int NewGoldSinceLastTrain { get; set; }

    // Thresholds (Pravila)
    public int RetrainGoldThreshold { get; set; } = 50;
    public float AutoThresholdHigh { get; set; } = 0.90f;
    public float AutoThresholdLow { get; set; } = 0.15f;

    public bool IsRetrainEnabled { get; set; } = true;
    public Guid? ActiveModelVersionId { get; set; }
}