using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Polls.Snapshots;

/// <summary>
/// One question and, for a root question, its NPS follow-ups. <see cref="Children"/> is empty on a
/// follow-up: the client reads exactly one level of nesting.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PollQuestionSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required int SortOrder { get; init; }

    [Id(2)]
    public required PollQuestionType QuestionType { get; init; }

    [Id(3)]
    public required string QuestionText { get; init; }

    /// <summary>Branch key matched against the parent choice's <see cref="PollChoiceSnapshot.ChoiceType"/>.</summary>
    [Id(4)]
    public required int QuestionCategory { get; init; }

    [Id(5)]
    public required int QuestionAnswerType { get; init; }

    /// <summary>
    /// Empty unless <see cref="QuestionType"/> is <see cref="PollQuestionType.SingleChoice"/> or
    /// <see cref="PollQuestionType.MultipleChoice"/> — the client reads a choice list only for those.
    /// </summary>
    [Id(6)]
    public ImmutableArray<PollChoiceSnapshot> Choices { get; init; } =
        ImmutableArray<PollChoiceSnapshot>.Empty;

    /// <summary>Follow-up questions; only ever populated on a root question.</summary>
    [Id(7)]
    public ImmutableArray<PollQuestionSnapshot> Children { get; init; } =
        ImmutableArray<PollQuestionSnapshot>.Empty;
}
