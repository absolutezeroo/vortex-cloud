using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;

namespace Vortex.Events;

public sealed class EventSystem(EventRegistry registry)
    : IEventPublisher,
        ICancellableEventPublisher
{
    private readonly EventRegistry _registry = registry;

    public async Task PublishAsync(IEvent @event, CancellationToken ct = default)
    {
        if (_registry is null)
        {
            return;
        }

        await _registry.PublishAsync(@event, null, ct).ConfigureAwait(false);
    }

    public async Task<EventContext> PublishCancellableAsync(
        IEvent @event,
        CancellationToken ct = default
    )
    {
        return await _registry.PublishWithContextAsync(@event, null, ct).ConfigureAwait(false);
    }
}
