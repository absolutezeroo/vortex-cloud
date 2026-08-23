using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Crafting;

[GenerateSerializer, Immutable]
public sealed record CraftingRecipeMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
