using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Vortex.Furniture;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Groups;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Pets;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Inventory.Fulfillment;

/// <summary>
/// Turns a catalog offer into the <see cref="FulfillmentPlan"/> it promises.
/// </summary>
/// <remarks>
/// <para>
/// Pure and deterministic on purpose. It reads the offer, the definition provider and the buyer's
/// guild identity, and returns data — no database, no grains, no clock. Which means it can run
/// before the wallet is touched, and every error it can raise is one the purchase never has to
/// compensate for.
/// </para>
/// <para>
/// It is also, finally, testable. The rules living in here have each been a bug: the copies granted
/// are the purchase quantity times the product's own count (ignoring the second collapsed every
/// bundle to one of each item); only string-array furni may carry a guild layout (stamping anything
/// else corrupts its stuff data); a bot without a figure is skipped rather than granted blank.
/// </para>
/// </remarks>
public sealed class CatalogFulfillmentPlanner(
    IFurnitureDefinitionProvider definitions,
    ILogger<CatalogFulfillmentPlanner> logger
)
{
    private readonly IFurnitureDefinitionProvider _definitions = definitions;
    private readonly ILogger<CatalogFulfillmentPlanner> _logger = logger;

    /// <summary>
    /// Works out everything <paramref name="offer"/> owes the buyer.
    /// </summary>
    /// <param name="guildIdentity">
    /// The buyer's identity in the guild named by <paramref name="extraParam"/>, resolved by the
    /// caller because it is a grain call and this stays pure.
    /// </param>
    /// <exception cref="VortexException">A product names a definition that does not exist.</exception>
    public FulfillmentPlan Plan(
        CatalogOfferSnapshot offer,
        string extraParam,
        int quantity,
        GuildFurniIdentitySnapshot? guildIdentity
    )
    {
        quantity = Math.Max(1, quantity);

        List<PlannedFurniture> furniture = [];
        List<string> badgeCodes = [];
        List<PetCreateRequest> pets = [];
        List<BotCreateRequest> bots = [];
        List<PlannedEffect> effects = [];

        foreach (CatalogProductSnapshot product in offer.Products)
        {
            switch (product.ProductType)
            {
                case ProductType.Floor:
                case ProductType.Wall:
                    PlanFurniture(product, quantity, guildIdentity, furniture);
                    break;

                case ProductType.Badge when !string.IsNullOrWhiteSpace(product.ExtraParam):
                    badgeCodes.Add(product.ExtraParam);
                    break;

                case ProductType.Effect when !string.IsNullOrWhiteSpace(product.ExtraParam):
                    PlanEffects(product, quantity, effects);
                    break;

                case ProductType.Robot:
                    PlanBot(product, extraParam, bots);
                    break;

                case ProductType.Pet:
                    pets.Add(PlanPet(product, extraParam));
                    break;

                default:
                    break;
            }
        }

        return new FulfillmentPlan
        {
            Furniture = furniture,
            BadgeCodes = badgeCodes,
            Pets = pets,
            Bots = bots,
            Effects = effects,
        };
    }

    private void PlanFurniture(
        CatalogProductSnapshot product,
        int quantity,
        GuildFurniIdentitySnapshot? guildIdentity,
        List<PlannedFurniture> into
    )
    {
        FurnitureDefinitionSnapshot def =
            _definitions.TryGetDefinition(product.FurniDefinitionId)
            ?? throw new VortexException(VortexErrorCodeEnum.FurnitureDefinitionNotFound);

        // Only string-array furni can carry the guild layout; stamping anything else would corrupt
        // its own stuff data.
        string? guildExtraData =
            guildIdentity is not null && def.StuffDataType == StuffDataType.StringKey
                ? BuildGuildExtraData(guildIdentity)
                : null;

        // Each product carries its own per-offer count (a bundle is an offer holding several
        // products, and any product may bundle >1 of an item), so the copies to grant are the
        // purchase multiplier times that count. Ignoring product.Quantity collapsed every bundle to
        // one of each item.
        int copies = quantity * Math.Max(1, product.Quantity);

        for (int i = 0; i < copies; i++)
        {
            into.Add(new PlannedFurniture(def.Id, guildExtraData));
        }
    }

    /// <summary>
    /// Reads an effect product. ExtraParam encodes it as "effectId", "effectId:durationSeconds", or
    /// "effectId:durationSeconds:subType" — duration 0 or absent means permanent. One grant per copy.
    /// </summary>
    private static void PlanEffects(
        CatalogProductSnapshot product,
        int quantity,
        List<PlannedEffect> into
    )
    {
        string[] fx = product.ExtraParam!.Split(':');

        if (!int.TryParse(fx[0], out int effectId) || effectId <= 0)
        {
            return;
        }

        int duration = fx.Length > 1 && int.TryParse(fx[1], out int d) ? d : 0;
        int subType = fx.Length > 2 && int.TryParse(fx[2], out int s) ? s : 0;
        int copies = quantity * Math.Max(1, product.Quantity);

        for (int i = 0; i < copies; i++)
        {
            into.Add(new PlannedEffect(effectId, subType, duration));
        }
    }

    private void PlanBot(
        CatalogProductSnapshot product,
        string purchaseExtraParam,
        List<BotCreateRequest> into
    )
    {
        BotCreateRequest? bot = BotProductReader.TryRead(product.ExtraParam, purchaseExtraParam);

        if (bot is null)
        {
            _logger.LogWarning(
                "Catalog product {ProductId} is a Robot but its extra param '{ExtraParam}' carries "
                    + "no figure; skipping the grant.",
                product.Id,
                product.ExtraParam
            );

            return;
        }

        into.Add(bot);
    }

    /// <summary>
    /// Reads a pet product. The purchase's own extra param carries the name, race and colour the
    /// player chose, newline-separated; the product's carries the pet type.
    /// </summary>
    private static PetCreateRequest PlanPet(
        CatalogProductSnapshot product,
        string purchaseExtraParam
    )
    {
        _ = int.TryParse(product.ExtraParam, out int petType);

        string[] parts = purchaseExtraParam.Split('\n');
        string petName = parts.Length > 0 ? parts[0].Trim() : "Pet";
        int race = parts.Length > 1 && int.TryParse(parts[1], out int r) ? r : 0;
        string color = parts.Length > 2 ? parts[2].Trim() : "ffffff";

        if (string.IsNullOrWhiteSpace(petName))
        {
            petName = "Pet";
        }

        return new PetCreateRequest
        {
            Name = petName,
            Type = petType,
            Race = race,
            Color = color,
            Gender = AvatarGenderType.Male,
            Energy = 100,
            Nutrition = 100,
        };
    }

    /// <summary>
    /// Serializes the guild layout into the item's extra-data blob, in the same "stuff" section
    /// shape the stuff-data factory reads back.
    /// </summary>
    internal static string BuildGuildExtraData(GuildFurniIdentitySnapshot identity)
    {
        ExtraData extraData = new(null);

        extraData.UpdateSection(
            ExtraDataSectionType.STUFF,
            new
            {
                Data = GuildFurniStuffData.Build(
                    identity.GroupId,
                    identity.BadgeCode,
                    identity.ColorOneHex,
                    identity.ColorTwoHex
                ),
            }
        );

        return extraData.GetJsonString();
    }
}
