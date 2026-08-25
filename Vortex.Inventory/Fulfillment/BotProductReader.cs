using System;
using System.Collections.Generic;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Inventory.Fulfillment;

/// <summary>Reads the bot a catalog product describes.</summary>
public static class BotProductReader
{
    /// <summary>
    /// Reads a bot product's definition. Habbo writes these as semicolon-separated key:value pairs
    /// — <c>name:Robbie;figure:hd-180-1...;gender:m;motto:...</c> — and a figure string itself
    /// contains neither separator, so the keys are unambiguous.
    /// <para>
    /// A bare figure with no keys is also accepted, because that is what a hand-written product
    /// looks like and rejecting it would be a trap rather than a rule.
    /// </para>
    /// </summary>
    /// <returns>Null when no figure could be found, which is the one field a bot cannot do without.</returns>
    public static BotCreateRequest? TryRead(string? productExtraParam, string purchaseExtraParam)
    {
        string definition = productExtraParam ?? string.Empty;

        Dictionary<string, string> fields = new(StringComparer.OrdinalIgnoreCase);
        string? bareFigure = null;

        foreach (string part in definition.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = part.IndexOf(':', StringComparison.Ordinal);

            if (separator <= 0)
            {
                bareFigure ??= part.Trim();
                continue;
            }

            fields[part[..separator].Trim()] = part[(separator + 1)..].Trim();
        }

        string figure = fields.GetValueOrDefault("figure", bareFigure ?? string.Empty);

        if (string.IsNullOrWhiteSpace(figure))
        {
            return null;
        }

        // The product names the bot; Habbo does not ask the buyer for one the way it does for a
        // pet. A typed name is still honoured if the product left the field out.
        string typedName = purchaseExtraParam.Split('\n')[0].Trim();
        string name = fields.GetValueOrDefault("name", string.Empty);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(typedName) ? "Bot" : typedName;
        }

        return new BotCreateRequest
        {
            Name = name,
            Figure = figure,
            Gender = fields
                .GetValueOrDefault("gender", "m")
                .StartsWith("f", StringComparison.OrdinalIgnoreCase)
                ? AvatarGenderType.Female
                : AvatarGenderType.Male,
            Motto = fields.GetValueOrDefault("motto", string.Empty),
        };
    }
}
