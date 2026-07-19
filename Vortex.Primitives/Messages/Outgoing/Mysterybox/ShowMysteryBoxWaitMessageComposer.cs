using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Mysterybox;

[GenerateSerializer, Immutable]
public sealed record ShowMysteryBoxWaitMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
