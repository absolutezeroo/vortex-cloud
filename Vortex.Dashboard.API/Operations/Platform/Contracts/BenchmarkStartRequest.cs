using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// The load a run should apply. Bounds are checked at the endpoint rather than here — a plan asking
/// for a million players is a typo, and the refusal should come before the audit rather than after.
/// </summary>
public sealed record BenchmarkStartRequest(
    int Players,
    int Furniture,
    int[]? FurnitureIds,
    int RoomId,
    int DurationSeconds,
    int RampSeconds,
    int WalkIntervalMs,
    int ChatIntervalMs,
    string? Label,
    string Reason
) : IReasonedRequest;
