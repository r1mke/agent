namespace AiAgents.BeeHiveAgent.Domain.Enums;

public enum TaskType
{
    Pollen = 1,
    Health = 2
}

public enum SampleStatus
{
    Queued = 0,
    Processing = 1,
    Scored = 2,
    PendingReview = 3,
    Reviewed = 4,
    Failed = 99
}

public enum Decision
{
    None = 0,
    AutoAccept = 1,
    PendingReview = 2,
    Alert = 3,
    AutoReject = 4
}