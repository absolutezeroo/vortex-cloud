using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Vortex.Database.Entities.Gamedata;

/// <summary>
/// A Trax song: what a song disk carries, what a jukebox plays, and what the client asks about by id
/// before it will play anything.
/// </summary>
/// <remarks>
/// <para>
/// The client holds no song metadata of its own. It receives a song id — from a disk in the hand,
/// from a jukebox playlist, from a catalogue page — and then asks <c>GetSongInfo</c> for every id it
/// does not know yet, on a one-second timer, batched. Until that answer arrives the song is drawn as
/// a nameless entry and never played, which is exactly what a hotel with no <c>songs</c> rows sees.
/// </para>
/// <para>
/// <see cref="LengthMs" /> is milliseconds because the wire is milliseconds: the client divides by
/// 1000 to get the seconds it schedules playback with. Storing seconds here would look tidier and
/// silently make every song a thousand times too long.
/// </para>
/// <para>
/// <see cref="OfficialSongId" /> is the external code an official song is published under, and the
/// only thing the catalogue knows about a song disk offer: the product's <c>extraParam</c> carries
/// that string, the client sends it back in <c>GetOfficialSongId</c>, and the answer is the numeric
/// id everything else in this feature speaks. It is empty for a song composed in-hotel.
/// </para>
/// </remarks>
[Table("songs")]
[Index(nameof(OfficialSongId))]
public class SongEntity : VortexEntity
{
    [Column("name")]
    public required string Name { get; set; }

    /// <summary>Shown as the song's author in the client. Free text, not a player reference: an
    /// official song's creator is a label, not an account.</summary>
    [Column("creator")]
    public required string Creator { get; set; }

    [Column("length_ms")]
    public required int LengthMs { get; set; }

    [Column("official_song_id")]
    public string OfficialSongId { get; set; } = string.Empty;

    /// <summary>The Trax composition itself — the sample list the client's sound machine replays.
    /// Empty for a song the client streams by <see cref="OfficialSongId" /> instead.</summary>
    /// <remarks>
    /// <c>text</c>, not the schema's default <c>varchar(512)</c>: a composition is a sample list, and
    /// a real one runs well past 512 characters. Truncating it costs the second half of the song and
    /// nothing says so.
    /// </remarks>
    [Column("data", TypeName = "text")]
    public string Data { get; set; } = string.Empty;
}
