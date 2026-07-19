using Vortex.Primitives.Messages.Incoming.Crafting;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Parsers.Crafting;

internal class GetCraftingRecipeMessageParser : IParser
{
    public IMessageEvent Parse(IClientPacket packet) => new GetCraftingRecipeMessage();
}
