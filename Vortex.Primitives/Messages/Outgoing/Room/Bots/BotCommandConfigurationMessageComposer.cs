using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Bots;

[GenerateSerializer, Immutable]
public sealed record BotCommandConfigurationMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
