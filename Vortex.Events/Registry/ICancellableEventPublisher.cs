using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Events;

namespace Vortex.Events.Registry;

public interface ICancellableEventPublisher
{
    Task<EventContext> PublishCancellableAsync(IEvent @event, CancellationToken ct = default);
}
