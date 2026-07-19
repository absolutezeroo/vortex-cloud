using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Game.Snowwar.Arena;

[GenerateSerializer, Immutable]
public sealed record Game2StageStillLoadingMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
