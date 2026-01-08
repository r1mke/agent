using AiAgents.BeeHiveAgent.Domain.Enums;
using AiAgents.Core.Abstractions; // <--- NOVO

namespace AiAgents.BeeHiveAgent.Domain.Entities;

// Dodajemo : IPercept
public class HiveImageSample : IPercept
{
    public Guid Id { get; set; }
    public Guid HiveId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
    public string? Label { get; set; }
    public TaskType TaskType { get; set; }
    public SampleStatus Status { get; set; }

    public virtual ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

    public void MarkProcessing()
    {
        if (Status != SampleStatus.Queued)
            throw new InvalidOperationException($"Cannot process sample in status {Status}");

        Status = SampleStatus.Processing;
    }
}