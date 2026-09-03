using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Sound;

/// <summary>
/// Loads one of the sender's song disks into the room's jukebox.
/// </summary>
/// <remarks>
/// The slot the client picked is carried but not honoured: Habbo's own editor appends, the playlist
/// has no gaps to fill, and taking the number would let a client claim a position that does not
/// exist. It is read so the layout stays right, and ignored on purpose.
/// </remarks>
public record AddJukeboxDiskMessage : IMessageEvent
{
    public required int DiskItemId { get; init; }

    public required int SlotNumber { get; init; }
}
