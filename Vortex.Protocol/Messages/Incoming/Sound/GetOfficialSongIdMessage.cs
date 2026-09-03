using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Sound;

/// <summary>
/// Asks which numeric song id an official song code stands for.
/// </summary>
/// <remarks>
/// Sent from the catalogue when a song-disk offer is selected: the product's <c>extraParam</c> is
/// the code, and until the answer comes back the client has no id to ask <c>GetSongInfo</c> about,
/// so the page shows a disk with no title and no preview.
/// </remarks>
public record GetOfficialSongIdMessage : IMessageEvent
{
    public required string OfficialSongId { get; init; }
}
