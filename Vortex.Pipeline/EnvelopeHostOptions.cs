using System;
using System.Threading.Tasks;

namespace Vortex.Pipeline;

public sealed class EnvelopeHostOptions<TEnvelope, TMeta, TContext>
{
    public Func<TEnvelope, TMeta?, Task<TContext>> CreateContextAsync { get; init; } = default!;
    public Func<TContext, bool>? ShouldShortCircuit { get; init; }
    public bool EnableInheritanceDispatch { get; init; } = true;
    public HandlerExecutionMode HandlerMode { get; init; } = HandlerExecutionMode.Parallel;
    public int? MaxHandlerDegreeOfParallelism { get; init; } = null;

    public Action<Exception, object>? OnHandlerActivationError { get; init; }
    public Action<Exception, object>? OnBehaviorActivationError { get; init; }
    public Action<Exception, object>? OnHandlerInvokeError { get; init; }
    public Action<Exception, object>? OnBehaviorInvokeError { get; init; }

    /// <summary>
    /// Invoked when an envelope reaches the end of the pipeline with zero registered handlers (after
    /// any behaviors ran). Unset by default — callers for whom "nobody is listening" is a normal,
    /// expected occurrence (e.g. optional domain-event hooks) should leave this null.
    /// </summary>
    public Action<TEnvelope>? OnNoHandlerRegistered { get; init; }
}
