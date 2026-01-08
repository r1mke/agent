namespace AiAgents.BeeHiveAgent.Web.DTOs;

public class ReviewRequestDto
{
    public Guid SampleId { get; set; }
    public bool IsPollen { get; set; } // Tvoja presuda: Da li je ovo polen?
}