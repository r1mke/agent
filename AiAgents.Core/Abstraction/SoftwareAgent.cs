namespace AiAgents.Core.Abstractions;

// Bazna klasa koja definiše ciklus
public abstract class SoftwareAgent<TPercept, TAction, TResult>
    where TPercept : IPercept
    where TAction : IAction
    where TResult : IResult
{
    // Glavna metoda koju Worker zove u petlji (Tick)
    // Vraća null ako nema posla (NoWork)
    public abstract Task<TResult?> StepAsync(CancellationToken ct);
}