using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Sound;

/// <summary>
/// Takes a disk back out of the room's jukebox, named by its place in the playlist the client was
/// last sent.
/// </summary>
public record RemoveJukeboxDiskMessage : IMessageEvent
{
    public required int Index { get; init; }
}
