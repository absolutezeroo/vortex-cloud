using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>The other side has started or stopped typing.</summary>
[GenerateSerializer, Immutable]
public sealed record GuideSessionPartnerIsTypingMessageComposer : IComposer
{
    [Id(0)]
    public required bool IsTyping { get; init; }
}
