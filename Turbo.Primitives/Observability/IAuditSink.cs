namespace Turbo.Primitives.Observability;

/// <summary>
/// Entry point for durable business audit. Implementations MUST be non-blocking (enqueue and
/// return) and MUST NOT perform synchronous database writes on the caller's thread. This keeps
/// audit strictly separate from technical logs and from metrics. The default implementation is a
/// no-op until the channel-backed writer is introduced in a later phase.
/// </summary>
public interface IAuditSink
{
    void Emit(in AuditEvent auditEvent);
}
