using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>One forum post (message). Mirrors the client MessageData (state is written as a byte).</summary>
[GenerateSerializer, Immutable]
public sealed record ForumPostSnapshot
{
    [Id(0)]
    public required int MessageId { get; init; }

    [Id(1)]
    public required int MessageIndex { get; init; }

    [Id(2)]
    public required int AuthorId { get; init; }

    [Id(3)]
    public required string AuthorName { get; init; }

    [Id(4)]
    public required string AuthorFigure { get; init; }

    [Id(5)]
    public required int CreationTimeAsSecondsAgo { get; init; }

    [Id(6)]
    public required string MessageText { get; init; }

    /// <summary>Post state (0 visible, 1 hidden, 2 hidden-by-admin) — serialized as a byte.</summary>
    [Id(7)]
    public required int State { get; init; }

    [Id(8)]
    public required int AdminId { get; init; }

    [Id(9)]
    public required string AdminName { get; init; }

    [Id(10)]
    public required int AdminOperationTimeAsSecondsAgo { get; init; }

    [Id(11)]
    public required int AuthorPostCount { get; init; }
}
