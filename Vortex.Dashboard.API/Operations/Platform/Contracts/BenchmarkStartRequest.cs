using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations.Platform.Contracts;

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
) : IReasonedRequest
{
    /// <summary>
    /// How often one synthetic player drags a piece of the room's furniture, in milliseconds.
    /// </summary>
    public int MoveIntervalMs { get; init; }

    /// <summary>How often one synthetic player clicks a piece of furniture.</summary>
    public int UseIntervalMs { get; init; }

    /// <summary>How often one synthetic player buys from the catalogue.</summary>
    public int BuyIntervalMs { get; init; }

    /// <summary>How often one synthetic player sends a friend an instant message.</summary>
    public int MessageIntervalMs { get; init; }

    /// <summary>How often one synthetic player creates a room of its own.</summary>
    public int CreateRoomIntervalMs { get; init; }

    /// <summary>
    /// How many rooms to spread the players over. One — the default — is the worst case: everyone
    /// queues behind a single Orleans grain. Several is what a hotel looks like.
    /// </summary>
    public int Rooms { get; init; } = 1;

    /// <summary>How often a synthetic player walks to another room, in milliseconds.</summary>
    public int RoomSwitchIntervalMs { get; init; }
}
