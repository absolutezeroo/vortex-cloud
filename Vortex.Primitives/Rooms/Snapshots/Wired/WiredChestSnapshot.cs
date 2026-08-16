using Orleans;

namespace Vortex.Primitives.Rooms.Snapshots.Wired;

/// <summary>What a wired chest is holding, as the room hands it to whoever opened it.</summary>
[GenerateSerializer, Immutable]
public sealed record WiredChestSnapshot
{
    /// <summary>The chest furni's id, which is the id the client echoes back on every chest
    /// request.</summary>
    [Id(0)]
    public required int ChestId { get; init; }

    [Id(1)]
    public required int Credits { get; init; }

    /// <summary>Whether this chest stores currency rather than furniture — the two halves have
    /// separate screens in the client, and separate messages.</summary>
    [Id(2)]
    public required bool IsCoinChest { get; init; }
}
