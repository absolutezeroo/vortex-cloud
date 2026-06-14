using Microsoft.Extensions.Logging;

namespace Turbo.Observability.Diagnostics;

/// <summary>
/// Centrally-allocated, stable <see cref="EventId"/>s. Ranges are reserved per concern so ids never
/// collide and can be referenced from log queries and dashboards:
/// <list type="bullet">
/// <item>1000-1099 observability core</item>
/// <item>1100-1199 networking / packet pipeline</item>
/// <item>1200-1299 audit pipeline</item>
/// <item>2000-2099 authentication</item>
/// <item>3000-3099 economy</item>
/// </list>
/// New ids are appended here rather than declared inline at the call site.
/// </summary>
public static class TurboEventIds
{
    public static readonly EventId ObservabilityReady = new(1000, nameof(ObservabilityReady));
    public static readonly EventId TraceScopeFault = new(1001, nameof(TraceScopeFault));
    public static readonly EventId GrainContextFault = new(1002, nameof(GrainContextFault));

    public static readonly EventId AuditDropped = new(1200, nameof(AuditDropped));
    public static readonly EventId AuditWriteFailed = new(1201, nameof(AuditWriteFailed));
}
