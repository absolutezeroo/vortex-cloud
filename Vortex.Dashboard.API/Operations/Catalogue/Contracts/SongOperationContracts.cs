using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the song catalogue, each carrying a mandatory audited <c>Reason</c>.
/// </summary>
/// <remarks>
/// Length is taken in seconds because that is what an operator has in front of them — the page shows
/// a track of 2:08 as 128 — and converted to the milliseconds the wire and the table use at the
/// boundary. <c>OfficialSongId</c> is the code a catalogue song-disk offer carries in its
/// <c>extraParam</c>; it is empty for a song composed in-hotel.
/// </remarks>
public sealed record CreateSongRequest(
    string Name,
    string Creator,
    int LengthSeconds,
    string OfficialSongId,
    string Data,
    string Reason
) : IReasonedRequest;

public sealed record UpdateSongRequest(
    int SongId,
    string Name,
    string Creator,
    int LengthSeconds,
    string OfficialSongId,
    string Data,
    string Reason
) : IReasonedRequest;

public sealed record DeleteSongRequest(int SongId, string Reason) : IReasonedRequest;

public sealed record ReloadSongsRequest(string Reason) : IReasonedRequest;
