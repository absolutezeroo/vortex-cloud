namespace Vortex.Primitives.Sound;

/// <summary>
/// What a song disk carries: the id of its song, as a number in its legacy stuff data.
/// </summary>
/// <remarks>
/// The client reads exactly this — <c>FurnitureSongDiskLogic</c> takes the furniture's
/// <c>furniture_extras</c> string and casts it to an int — so anything else on a disk is not a song
/// id. Two places ask the question, the player's hand and a jukebox's playlist, and they reach the
/// string by different routes; the parsing rule lives here so it cannot answer differently.
/// </remarks>
public static class SongDiskExtraData
{
    /// <summary>The song on the disk, or 0 when the disk carries no usable id.</summary>
    public static int ReadSongId(string? legacyData) =>
        int.TryParse(legacyData, out int songId) && songId > 0 ? songId : 0;
}
