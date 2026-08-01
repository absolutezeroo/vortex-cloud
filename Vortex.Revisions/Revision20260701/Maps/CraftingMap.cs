using Vortex.Primitives.Networking.Revisions;
using Vortex.Revisions.Revision20260701.Parsers.Crafting;

namespace Vortex.Revisions.Revision20260701.Maps;

internal sealed class CraftingMap : IRevisionMap
{
    public void RegisterInto(IRevisionMapBuilder builder)
    {
        builder.MapParser(MessageEvent.CraftEvent, new CraftMessageParser());
        builder.MapParser(MessageEvent.CraftSecretEvent, new CraftSecretMessageParser());
        builder.MapParser(
            MessageEvent.GetCraftableProductsEvent,
            new GetCraftableProductsMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetCraftingRecipeEvent,
            new GetCraftingRecipeMessageParser()
        );
        builder.MapParser(
            MessageEvent.GetCraftingRecipesAvailableEvent,
            new GetCraftingRecipesAvailableMessageParser()
        );
    }
}
