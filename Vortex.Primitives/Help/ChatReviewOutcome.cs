using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Help;

/// <summary>
/// What a step of a chat review produced: who to offer it to, whose vote to acknowledge, and the
/// verdict once everyone who took it has voted.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewOutcome
{
    /// <summary>Guardians the review has just been put in front of.</summary>
    [Id(0)]
    public ImmutableArray<int> OfferedTo { get; init; } = [];

    /// <summary>Everyone currently holding it, for the voting-status refresh they all share.</summary>
    [Id(1)]
    public ImmutableArray<int> Participants { get; init; } = [];

    /// <summary>The chat being judged, sent with the offer.</summary>
    [Id(2)]
    public string ChatRecord { get; init; } = string.Empty;

    /// <summary>Set once every guardian who accepted has voted.</summary>
    [Id(3)]
    public ChatReviewResultSnapshot? Result { get; init; }

    public bool Nothing => OfferedTo.IsEmpty && Participants.IsEmpty && Result is null;
}

/// <summary>The verdict, plus each guardian's own vote so their client can show what they picked.</summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewResultSnapshot
{
    /// <summary>0 is "this chat was fine", 1 is "this chat was abusive" — the client's two
    /// buttons.</summary>
    [Id(0)]
    public required int WinningVote { get; init; }

    [Id(1)]
    public required ImmutableArray<int> Votes { get; init; }

    /// <summary>Guardian id to the vote they cast, so each is told their own back.</summary>
    [Id(2)]
    public required ImmutableDictionary<int, int> VotesByGuardian { get; init; }
}
