using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Events;

/// <summary>
/// Publishes a domain event to the in-process event bus. The publish contract lives in
/// <c>Turbo.Primitives</c> so any module or plugin can raise (and, via <c>IEventHandler&lt;T&gt;</c>,
/// observe) domain events without referencing the event runtime directly.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IEvent @event, CancellationToken ct = default);
}
