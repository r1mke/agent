namespace AiAgents.Core.Abstractions;


public abstract class SoftwareAgent<TPercept, TAction, TResult>
    where TPercept : IPercept
    where TAction : IAction
    where TResult : IResult
{

    public abstract Task<TResult?> StepAsync(CancellationToken ct);
}