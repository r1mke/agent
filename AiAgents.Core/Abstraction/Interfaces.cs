namespace AiAgents.Core.Abstractions;

public interface IPercept { }
public interface IAction { }
public interface IResult { }
public interface IExperience { }

// Uklonili smo 'out' ispred TPercept jer Task<T> ne podržava varijansu
public interface IPerceptionSource<TPercept> where TPercept : IPercept
{
    Task<TPercept?> PerceiveAsync(CancellationToken ct);
}

// Uklonili smo 'in' i 'out'
public interface IActuator<TAction, TResult>
    where TAction : IAction
    where TResult : IResult
{
    Task<TResult> ActAsync(TAction action, CancellationToken ct);
}
