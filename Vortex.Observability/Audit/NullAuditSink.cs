using Vortex.Primitives.Observability;

namespace Vortex.Observability.Audit;

/// <summary>
/// Default, intentionally inert <see cref="IAuditSink"/>. The audit abstraction is wired across the
/// codebase from the first brick so call sites can be added incrementally, but durable persistence
/// is introduced in a later phase. Swapping this registration for a channel-backed writer is the
/// only change required to start persisting audit events.
/// </summary>
public sealed class NullAuditSink : IAuditSink
{
    public void Emit(in AuditEvent auditEvent) { }
}
