namespace Vortex.Primitives.Sound.Admin;

/// <summary>
/// One song as an operator edits it.
/// </summary>
/// <remarks>
/// <see cref="LengthMs" /> is milliseconds because the wire is milliseconds. An operator thinks in
/// seconds, so the page converts — not this record, which stays in the unit everything else uses.
/// </remarks>
public sealed record SongSpec(
    string Name,
    string Creator,
    int LengthMs,
    string OfficialSongId,
    string Data
);

/// <summary>The outcome of one song admin write.</summary>
public sealed record SongAdminResult
{
    public required bool Success { get; init; }

    /// <summary>A machine-readable reason, empty on success.</summary>
    public required string Error { get; init; }

    /// <summary>The row that was written, when there is one.</summary>
    public int SongId { get; init; }

    public static SongAdminResult Ok(int songId) =>
        new()
        {
            Success = true,
            Error = string.Empty,
            SongId = songId,
        };

    public static SongAdminResult Fail(string error) => new() { Success = false, Error = error };
}
