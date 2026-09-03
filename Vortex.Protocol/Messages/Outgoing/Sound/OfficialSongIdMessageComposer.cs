using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Sound;

/// <summary>
/// Resolves an official song code to the numeric id the rest of the protocol speaks.
/// </summary>
/// <remarks>
/// The code goes back out unchanged because the client matches on it: the catalogue widget compares
/// it against the code it sent and ignores an answer that does not match, so a page the player has
/// already moved away from cannot overwrite the one they are looking at.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record OfficialSongIdMessageComposer : IComposer
{
    [Id(0)]
    public required string OfficialSongId { get; init; }

    [Id(1)]
    public required int SongId { get; init; }
}
