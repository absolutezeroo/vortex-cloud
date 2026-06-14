using System.Text.Json;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Dashboard.Http;

internal sealed class DashboardAuditEmitter(IAuditSink auditSink)
{
    private readonly IAuditSink _auditSink = auditSink;

    public void Emit(
        string path,
        AuditResult result,
        int status,
        string eventKind,
        DashboardRole role
    )
    {
        _auditSink.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Security,
                Action = "audit.viewed",
                Severity =
                    result == AuditResult.Success ? AuditSeverity.Info : AuditSeverity.Warning,
                Result = result,
                IpHash = null,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        path,
                        status,
                        role = role.ToString().ToLowerInvariant(),
                        kind = eventKind,
                    }
                ),
            }
        );
    }
}
