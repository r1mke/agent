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
        // 1. Ako je score veoma visok -> AutoAccept
        if (score >= settings.AutoThresholdHigh)
        {
            return (SampleStatus.Scored, Decision.AutoAccept, predictedLabel);
        }

        // 2. Ako je score veoma nizak (što znači da je suprotna klasa vjerovatna) -> AutoReject
        // Napomena: Ovo zavisi kako ML model vraća score. 
        // Ako je binarna klasifikacija (Score je vjerovatnoća za Pollen):
        // Score 0.1 znači 90% NoPollen.
        if (score <= settings.AutoThresholdLow)
        {
            return (SampleStatus.Scored, Decision.AutoReject, predictedLabel);
        }

        // 3. Sredina -> Nesigurno, treba ljudski pregled
        return (SampleStatus.PendingReview, Decision.PendingReview, predictedLabel);
    }
}