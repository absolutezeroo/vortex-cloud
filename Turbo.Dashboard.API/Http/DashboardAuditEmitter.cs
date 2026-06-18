using System.Text.Json;
using Turbo.Primitives.Observability;

namespace Turbo.Dashboard.API.Http;

internal sealed class DashboardAuditEmitter(IAuditSink auditSink)
{
    private readonly IAuditSink _auditSink = auditSink;

    public void Emit(string path, AuditResult result, int status, string eventKind, string actor)
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
                        actor,
                        kind = eventKind,
                    }
                ),
            }
        );
    }
}
