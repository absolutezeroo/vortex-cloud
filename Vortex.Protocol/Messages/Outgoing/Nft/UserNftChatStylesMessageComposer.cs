using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Nft;

[GenerateSerializer, Immutable]
public sealed record UserNftChatStylesMessageComposer : IComposer
{
    [Id(0)]
    public required List<int> ChatStyleIds { get; init; }
}
