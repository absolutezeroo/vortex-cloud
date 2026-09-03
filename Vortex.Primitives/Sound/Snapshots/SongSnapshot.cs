using Orleans;

namespace Vortex.Primitives.Sound.Snapshots;

/// <summary>
/// One Trax song as everything outside the database sees it.
/// </summary>
/// <remarks>
/// <see cref="LengthMs" /> is milliseconds, matching the wire and the client's own arithmetic.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record SongSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required string Creator { get; init; }

    [Id(3)]
    public required int LengthMs { get; init; }

    [Id(4)]
    public required string OfficialSongId { get; init; }

    [Id(5)]
    public required string Data { get; init; }
}
