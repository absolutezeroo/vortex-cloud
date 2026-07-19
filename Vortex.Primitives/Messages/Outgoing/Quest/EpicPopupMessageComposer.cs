using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Quest;

[GenerateSerializer, Immutable]
public sealed record EpicPopupMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
