using Microsoft.Extensions.Logging;
using Vortex.Events.Registry;
using Vortex.Pipeline;
using Vortex.Primitives.Events;

namespace Vortex.Events;

internal sealed class EventFeatureProcessor(
    EventRegistry registry,
    EnvelopeInvokerFactory<EventContext> invokerFactory,
    ILogger<EventFeatureProcessor> logger
)
    : EnvelopeFeatureProcessor<IEvent, object, EventContext>(
        registry,
        invokerFactory,
        typeof(IEventHandler<>),
        typeof(IEventBehavior<>),
        logger
    ) { }
