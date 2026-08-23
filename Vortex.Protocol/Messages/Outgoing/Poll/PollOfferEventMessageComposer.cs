using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Poll;

/// <summary>
/// Offers a survey to one player: the client shows the "got a minute?" dialog with
/// <see cref="Headline"/> and <see cref="Summary"/>, and answers with either PollStart (accept) or
/// PollReject (decline).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PollOfferEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PollId { get; init; }

    /// <summary>Operator-defined tag; the client stores it without branching on it.</summary>
    [Id(1)]
    public required string PollType { get; init; }

    [Id(2)]
    public required string Headline { get; init; }

    [Id(3)]
    public required string Summary { get; init; }
}
