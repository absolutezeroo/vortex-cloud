using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The marked quiz. Only the questions that were got wrong are named — the client shows the pass
/// screen when this list is empty and the review screen when it is not, so an empty list is the
/// pass signal rather than an absence of one.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record QuizResultsMessageComposer : IComposer
{
    [Id(0)]
    public required string QuizCode { get; init; }

    [Id(1)]
    public required ImmutableArray<int> WrongQuestionIds { get; init; }
}
