using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Events;

namespace Turbo.Events.Registry;

public interface ICancellableEventPublisher
{
    Task<EventContext> PublishCancellableAsync(IEvent @event, CancellationToken ct = default);
}
