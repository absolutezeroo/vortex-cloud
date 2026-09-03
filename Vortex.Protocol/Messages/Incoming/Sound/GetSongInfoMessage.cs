using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Sound;

/// <summary>
/// "Tell me about these songs." The client batches every id it has met and does not know yet — from
/// a disk, a playlist, a catalogue page — and sends them together on a one-second timer.
/// </summary>
public record GetSongInfoMessage : IMessageEvent
{
    public required ImmutableArray<int> SongIds { get; init; }
}
