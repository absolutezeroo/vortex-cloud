using System.Collections.Generic;

namespace Turbo.Events.Registry;

public sealed class EventContext
{
    public bool Cancel { get; set; }

    public string? CancelReason { get; set; }

    public required string CorrelationId { get; init; }

    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
