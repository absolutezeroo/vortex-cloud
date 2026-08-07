using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// A request has landed: on the guide's side it is the offer to take it, on the requester's side it
/// is confirmation that somebody is looking. One packet, told apart by <see cref="AsGuide"/>.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionAttachedMessageComposer : IComposer
{
    [Id(0)]
    public required bool AsGuide { get; init; }

    [Id(1)]
    public required int HelpRequestType { get; init; }

    [Id(2)]
    public required string HelpRequestDescription { get; init; }

    /// <summary>Seconds the client shows as "how long you have been waiting".</summary>
    [Id(3)]
    public required int RoleSpecificWaitTime { get; init; }
}
