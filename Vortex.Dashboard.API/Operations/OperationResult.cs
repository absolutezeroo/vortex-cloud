using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Outcome of a dashboard admin operation. Always carries the correlation id so the operator can
/// trace the action across logs, audit records and grain activity.
/// </summary>
public sealed record OperationResult(bool Ok, string CorrelationId, string Message)
{
    public static OperationResult Succeeded(string correlationId) => new(true, correlationId, "ok");

    public static OperationResult Failed(
        string correlationId,
        string message = "operation_failed"
    ) => new(false, correlationId, message);
}
