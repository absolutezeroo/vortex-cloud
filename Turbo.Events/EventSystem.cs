using System.Threading;
using System.Threading.Tasks;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;

namespace Turbo.Events;

public sealed class EventSystem(EventRegistry registry) : IEventPublisher
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
}
