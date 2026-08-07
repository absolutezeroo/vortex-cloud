using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// A chat review put in front of a guardian, with how long their client should count down before
/// giving up on them.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewSessionOfferedToGuideMessageComposer : IComposer
{
    [Id(0)]
    public required int AcceptanceTimeoutSeconds { get; init; }
}
