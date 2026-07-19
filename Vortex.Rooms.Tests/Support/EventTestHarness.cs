using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Events;
using Vortex.Events.Registry;
using Vortex.Pipeline.Delegates;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Rooms.Tests.Support;

internal sealed class EventTestHarness
{
    private static readonly IServiceProvider EMPTY_SERVICES = new EmptyServiceProvider();

    public EventTestHarness(string correlationId)
    {
        ErrorSink = new RecordingErrorGroupingSink();
        Registry = new EventRegistry(
            EMPTY_SERVICES,
            ErrorSink,
            new FixedContextAccessor(correlationId),
            NullLogger<EventRegistry>.Instance
        );
        System = new EventSystem(Registry);
    }

    public RecordingErrorGroupingSink ErrorSink { get; }

    public EventRegistry Registry { get; }

    public EventSystem System { get; }

    public void RegisterBehavior<TEvent>(IEventBehavior<TEvent> behavior, int order = 0)
        where TEvent : IEvent
    {
        BehaviorInvoker<EventContext> invoker = async (inst, env, ctx, next, ct) =>
            await ((IEventBehavior<TEvent>)inst)
                .InvokeAsync((TEvent)env, ctx, next, ct)
                .ConfigureAwait(false);

        Registry.RegisterBehavior(typeof(TEvent), EMPTY_SERVICES, _ => behavior, invoker, order);
    }

    public void RegisterHandler<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        HandlerInvoker<EventContext> invoker = async (inst, env, ctx, ct) =>
            await ((IEventHandler<TEvent>)inst)
                .HandleAsync((TEvent)env, ctx, ct)
                .ConfigureAwait(false);

        Registry.RegisterHandler(typeof(TEvent), EMPTY_SERVICES, _ => handler, invoker);
    }

    internal sealed class RecordingErrorGroupingSink : IErrorGroupingSink
    {
        public int Count { get; private set; }

        public string? LastSource { get; private set; }

        public void Record(
            Exception exception,
            string source,
            string operation,
            long? actorId = null,
            int? roomId = null,
            string? correlationId = null,
            string? sessionKey = null,
            string? remoteIp = null
        )
        {
            Count++;
            LastSource = source;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }

    private sealed class FixedContextAccessor(string correlationId) : IVortexContextAccessor
    {
        public IVortexContext? Current { get; private set; } =
            new VortexContext(new CorrelationId(correlationId), "test");

        public IVortexTraceScope BeginScope(
            string operation,
            string? sessionKey = null,
            CorrelationId? correlationId = null,
            long? playerId = null,
            int? roomId = null
        )
        {
            IVortexContext? previous = Current;
            Current = new VortexContext(
                correlationId ?? previous?.CorrelationId ?? CorrelationId.None,
                operation,
                sessionKey,
                playerId,
                roomId
            );

            return new Scope(this, previous);
        }

        private sealed class Scope(FixedContextAccessor accessor, IVortexContext? previous)
            : IVortexTraceScope
        {
            public IVortexContext Context { get; } = accessor.Current!;

            public void Dispose()
            {
                accessor.Current = previous;
            }
        }
    }
}
