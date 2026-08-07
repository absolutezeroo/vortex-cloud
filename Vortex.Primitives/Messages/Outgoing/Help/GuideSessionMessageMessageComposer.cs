using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// A line of chat, echoed to both sides with who said it. The sender is on the wire because the
/// client draws the two speakers differently and has no other way to tell them apart.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionMessageMessageComposer : IComposer
{
    [Id(0)]
    public required string ChatMessage { get; init; }

    [Id(1)]
    public required int SenderId { get; init; }
}
