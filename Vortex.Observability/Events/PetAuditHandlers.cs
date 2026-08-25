using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>
/// Pets as durable audit. A pet is a tradeable, nameable thing that lives in somebody's inventory,
/// and until now it moved between rooms and owners leaving nothing behind at all -- the one part of
/// the hotel's property that had no history.
/// </summary>
public sealed class PetAdoptedAuditHandler(IAuditSink audit) : IEventHandler<PetAdoptedEvent>
{
    public ValueTask HandleAsync(PetAdoptedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "pet.adopted",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.OwnerId,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.PetId,
                        e.Name,
                        e.Type,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PetPlacedAuditHandler(IAuditSink audit) : IEventHandler<PetPlacedEvent>
{
    public ValueTask HandleAsync(PetPlacedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "pet.placed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { e.PetId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class PetPickedUpAuditHandler(IAuditSink audit) : IEventHandler<PetPickedUpEvent>
{
    public ValueTask HandleAsync(PetPickedUpEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "pet.picked_up",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.ActorId,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { e.PetId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// No actor: a level is reached by the pet, and the person who fed it the last command is not
/// necessarily the one who trained it.
/// </summary>
public sealed class PetLeveledUpAuditHandler(IAuditSink audit) : IEventHandler<PetLeveledUpEvent>
{
    public ValueTask HandleAsync(PetLeveledUpEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "pet.leveled_up",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                RoomId = e.RoomId,
                Data = JsonSerializer.Serialize(new { e.PetId, e.Level }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
