using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>
/// A group forum's header data. Mirrors the client ForumData (base fields) plus ExtendedForumData
/// (permissions + per-viewer error strings). An empty *Error string means the viewer is allowed;
/// a non-empty one means denied (the client derives canRead/canPost/... from error length == 0).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ForumSnapshot
{
    [Id(0)]
    public required int GroupId { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required string Description { get; init; }

    [Id(3)]
    public required string Icon { get; init; }

    [Id(4)]
    public required int TotalThreads { get; init; }

    [Id(5)]
    public required int LeaderboardScore { get; init; }

    [Id(6)]
    public required int TotalMessages { get; init; }

    [Id(7)]
    public required int UnreadMessages { get; init; }

    [Id(8)]
    public required int LastMessageId { get; init; }

    [Id(9)]
    public required int LastMessageAuthorId { get; init; }

    [Id(10)]
    public required string LastMessageAuthorName { get; init; }

    [Id(11)]
    public required int LastMessageTimeAsSecondsAgo { get; init; }

    // ── Extended (ForumData single-forum view) ──────────────────────────────────
    [Id(12)]
    public required int ReadPermissions { get; init; }

    [Id(13)]
    public required int PostMessagePermissions { get; init; }

    [Id(14)]
    public required int PostThreadPermissions { get; init; }

    [Id(15)]
    public required int ModeratePermissions { get; init; }

    [Id(16)]
    public required string ReadPermissionError { get; init; }

    [Id(17)]
    public required string PostMessagePermissionError { get; init; }

    [Id(18)]
    public required string PostThreadPermissionError { get; init; }

    [Id(19)]
    public required string ModeratePermissionError { get; init; }

    [Id(20)]
    public required string ReportPermissionError { get; init; }

    [Id(21)]
    public required bool CanChangeSettings { get; init; }

    [Id(22)]
    public required bool IsStaff { get; init; }
}
