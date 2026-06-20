using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Turbo.Pipeline;
using Turbo.Primitives;
using Turbo.Primitives.Events;
using Turbo.Primitives.Observability;

namespace Turbo.Events.Registry;

public sealed class EventRegistry(
    IServiceProvider sp,
    IErrorGroupingSink errorGroupingSink,
    ITurboContextAccessor contextAccessor,
    ILogger<EventRegistry> logger
)
    : EnvelopeHost<IEvent, object, EventContext>(
        sp,
        CreateOptions(errorGroupingSink, contextAccessor, logger)
    )
{
    private static EnvelopeHostOptions<IEvent, object, EventContext> CreateOptions(
        IErrorGroupingSink errorGroupingSink,
        ITurboContextAccessor contextAccessor,
        ILogger<EventRegistry> logger
    )
    {
        return new EnvelopeHostOptions<IEvent, object, EventContext>
        {
            CreateContextAsync = (env, session) => Task.FromResult(new EventContext()),
            EnableInheritanceDispatch = true,
            HandlerMode = HandlerExecutionMode.Parallel,
            MaxHandlerDegreeOfParallelism = null,
            OnHandlerActivationError = (ex, env) =>
                ReportError(
                    ex,
                    "event-registry.activation",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnHandlerInvokeError = (ex, env) =>
                ReportError(
                    ex,
                    "event-registry.invoke",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnBehaviorActivationError = (ex, env) =>
                ReportError(
                    ex,
                    "event-registry.behavior-activation",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnBehaviorInvokeError = (ex, env) =>
                ReportError(
                    ex,
                    "event-registry.behavior-invoke",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
        };
    }

    private static void ReportError(
        Exception ex,
        string source,
        object env,
        IErrorGroupingSink errorGroupingSink,
        ITurboContextAccessor contextAccessor,
        ILogger logger
    )
    {
        ITurboContext? context = contextAccessor.Current;

        try
        {
            errorGroupingSink.Record(
                ex,
                source,
                env.GetType().Name,
                context?.PlayerId,
                context?.RoomId,
                context?.CorrelationId.Value,
                context?.SessionKey
            );

            logger.LogWarning(
                ex,
                "Event registry pipeline failure: {Source}/{Operation}",
                source,
                env.GetType().Name
            );
        }
        catch (Exception reportEx)
        {
            logger.LogError(
                reportEx,
                "Event registry failed to report a pipeline error for {Operation}",
                env.GetType().Name
            );
        }
    }
}
