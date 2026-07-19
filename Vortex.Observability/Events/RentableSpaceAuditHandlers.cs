using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>Records the start of a rental in the audit log.</summary>
public sealed class RentalStartedAuditHandler(IAuditSink audit) : IEventHandler<RentalStartedEvent>
{
    public ValueTask HandleAsync(RentalStartedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.RentableSpace,
                Action = "rentable_space.started",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.RenterId,
                RoomId = e.RoomId,
                ItemId = e.FurnitureId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        price = e.Price,
                        currency = e.Currency,
                        rentedUntil = e.RentedUntil,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records a natural rental expiry in the audit log.</summary>
public sealed class RentalExpiredAuditHandler(IAuditSink audit) : IEventHandler<RentalExpiredEvent>
{
    public ValueTask HandleAsync(RentalExpiredEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.RentableSpace,
                Action = "rentable_space.expired",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.RenterId,
                RoomId = e.RoomId,
                ItemId = e.FurnitureId,
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>Records an early cancellation in the audit log.</summary>
public sealed class RentalCancelledAuditHandler(IAuditSink audit)
    : IEventHandler<RentalCancelledEvent>
{
    public ValueTask HandleAsync(RentalCancelledEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.RentableSpace,
                Action = "rentable_space.cancelled",
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorPlayerId,
                TargetPlayerId = e.RenterId,
                RoomId = e.RoomId,
                ItemId = e.FurnitureId,
            }
        );

        return ValueTask.CompletedTask;
    }
}
