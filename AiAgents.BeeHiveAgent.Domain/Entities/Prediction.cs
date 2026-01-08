using AiAgents.BeeHiveAgent.Domain.Enums;
using AiAgents.Core.Abstractions; // <--- NOVO

namespace AiAgents.BeeHiveAgent.Domain.Entities;

public class Prediction : IAction
{
    public Guid Id { get; set; }
    public Guid SampleId { get; set; }
    public Guid ModelVersionId { get; set; }

    // PREIMENOVANO: Probability -> Score (da odgovara Runneru)
    public float Score { get; set; }

    public string PredictedLabel { get; set; } = string.Empty;

    public Decision Decision { get; set; }
    public DateTime CreatedAt { get; set; }
}