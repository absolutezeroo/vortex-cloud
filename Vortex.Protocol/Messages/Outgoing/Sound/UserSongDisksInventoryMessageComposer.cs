using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// Every song disk the player is holding, so the jukebox editor has something to offer.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record UserSongDisksInventoryMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<SongDiskSnapshot> Disks { get; init; }
}
