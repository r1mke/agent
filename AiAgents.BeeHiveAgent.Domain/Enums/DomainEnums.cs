namespace AiAgents.BeeHiveAgent.Domain.Enums;

public enum TaskType
{
    Pollen = 1,
    Health = 2
}

public enum SampleStatus
{
    Queued = 0,         // Tek uploadovano
    Processing = 1,     // Agent trenutno radi (lock)
    Scored = 2,         // Model dao predikciju
    PendingReview = 3,  // Nesiguran rezultat, čeka čovjeka
    Reviewed = 4,       // Čovjek potvrdio (Gold label)
    Failed = 99
}

public enum Decision
{
    None = 0,
    AutoAccept = 1,     // Visoka pouzdanost
    PendingReview = 2,  // Srednja pouzdanost
    Alert = 3,          // Za Health
    AutoReject = 4      // <--- NOVO: Dodano da popravi grešku u Policy
}