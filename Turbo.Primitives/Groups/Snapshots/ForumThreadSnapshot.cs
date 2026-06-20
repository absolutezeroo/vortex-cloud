using Orleans;

namespace Turbo.Primitives.Groups.Snapshots;

/// <summary>One forum thread row. Mirrors the client ThreadData (state is written as a byte).</summary>
[GenerateSerializer, Immutable]
public sealed record ForumThreadSnapshot
{
    [Id(0)]
    public required int ThreadId { get; init; }

    [Id(1)]
    public required int AuthorId { get; init; }

    [Id(2)]
    public required string AuthorName { get; init; }

    [Id(3)]
    public required string Subject { get; init; }

    [Id(4)]
    public required bool IsSticky { get; init; }

    [Id(5)]
    public required bool IsLocked { get; init; }

    [Id(6)]
    public required int CreationTimeAsSecondsAgo { get; init; }

    [Id(7)]
    public required int MessageCount { get; init; }

    [Id(8)]
    public required int UnreadMessageCount { get; init; }

    [Id(9)]
    public required int LastMessageId { get; init; }

    [Id(10)]
    public required int LastMessageAuthorId { get; init; }

    [Id(11)]
    public required string LastMessageAuthorName { get; init; }

    [Id(12)]
    public required int LastMessageTimeAsSecondsAgo { get; init; }

    /// <summary>Thread state (0 open, 1 locked, 2 hidden) — serialized as a byte.</summary>
    [Id(13)]
    public required int State { get; init; }

    [Id(14)]
    public required int AdminId { get; init; }

    [Id(15)]
    public required string AdminName { get; init; }

    [Id(16)]
    public required int AdminOperationTimeAsSecondsAgo { get; init; }
}
