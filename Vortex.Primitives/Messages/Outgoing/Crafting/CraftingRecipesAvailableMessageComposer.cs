using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Crafting;

[GenerateSerializer, Immutable]
public sealed record CraftingRecipesAvailableMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
