using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Bots;

/// <summary>Pops the bot's own menu open, so an owner who just placed one is not left hunting for
/// where its settings live.</summary>
[GenerateSerializer, Immutable]
public sealed record BotForceOpenContextMenuMessageComposer : IComposer
{
    [Id(0)]
    public required int BotId { get; init; }
}
