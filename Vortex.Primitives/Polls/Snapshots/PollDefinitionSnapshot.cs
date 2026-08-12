using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Polls.Snapshots;

/// <summary>A cached survey: one <c>polls</c> row with its question tree already assembled.</summary>
[GenerateSerializer, Immutable]
public sealed record PollDefinitionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required string Code { get; init; }

    [Id(2)]
    public required string PollType { get; init; }

    [Id(3)]
    public required string Headline { get; init; }

    [Id(4)]
    public required string Summary { get; init; }

    [Id(5)]
    public required string StartMessage { get; init; }

    [Id(6)]
    public required string EndMessage { get; init; }

    [Id(7)]
    public required bool NpsPoll { get; init; }

    /// <summary>True when the poll may be pushed on room entry.</summary>
    [Id(8)]
    public required bool OfferOnRoomEntry { get; init; }

    /// <summary>Room the offer is pinned to; null = any room.</summary>
    [Id(9)]
    public int? RoomId { get; init; }

    [Id(10)]
    public required int SortOrder { get; init; }

    /// <summary>Root questions in sort order, each carrying its own follow-ups.</summary>
    [Id(11)]
    public ImmutableArray<PollQuestionSnapshot> Questions { get; init; } =
        ImmutableArray<PollQuestionSnapshot>.Empty;
}
