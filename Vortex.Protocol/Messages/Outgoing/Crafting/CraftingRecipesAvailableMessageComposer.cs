using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Crafting;

[GenerateSerializer, Immutable]
public sealed record CraftingRecipesAvailableMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
