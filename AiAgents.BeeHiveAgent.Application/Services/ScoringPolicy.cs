using AiAgents.BeeHiveAgent.Domain.Entities;
using AiAgents.BeeHiveAgent.Domain.Enums;

namespace AiAgents.BeeHiveAgent.Application.Services;

public class ScoringPolicy
{
    public (SampleStatus NewStatus, Decision Decision, string Label) Evaluate(
        float score,
        string predictedLabel,
        SystemSettings settings)
    {

        if (score >= settings.AutoThresholdHigh)
        {
            return (SampleStatus.Scored, Decision.AutoAccept, predictedLabel);
        }


        if (score <= settings.AutoThresholdLow)
        {
            return (SampleStatus.Scored, Decision.AutoReject, predictedLabel);
        }


        return (SampleStatus.PendingReview, Decision.PendingReview, predictedLabel);
    }
}