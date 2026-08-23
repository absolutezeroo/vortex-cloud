using System.Collections.Immutable;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// A contract's terms, written the one way the client reads them.
/// </summary>
/// <remarks>
/// Two messages carry them — the offer a box makes, and the editor's own read-back — and the client
/// parses both with the same class. Shared for that reason: a field added to one writer and not the
/// other desynchronises whichever was forgotten, and neither would throw.
/// </remarks>
internal static class TradeContractWriter
{
    /// <summary>Coin terms go out as 0, furni terms as 1 — a byte, not an int.</summary>
    private const byte CoinNode = 0;

    private const byte FurniNode = 1;

    /// <summary>
    /// The definition: which alternatives pay for it, and what comes back.
    /// </summary>
    /// <remarks>
    /// Each side is announced by a flag before it, and a side that was never written is not the
    /// same as an empty one — the client keeps that distinction, so this does too.
    /// </remarks>
    public static IServerPacket WriteDefinition(
        this IServerPacket packet,
        ImmutableArray<TradeContractRule>? youGiveRules,
        TradeContractRule? youGetRule
    )
    {
        packet.WriteBoolean(youGiveRules is not null);

        if (youGiveRules is not null)
        {
            packet.WriteInteger(youGiveRules.Value.Length);

            foreach (TradeContractRule rule in youGiveRules.Value)
            {
                WriteRule(packet, rule);
            }
        }

        packet.WriteBoolean(youGetRule is not null);

        if (youGetRule is not null)
        {
            WriteRule(packet, youGetRule);
        }

        return packet;
    }

    private static void WriteRule(IServerPacket packet, TradeContractRule rule)
    {
        packet.WriteInteger(rule.Nodes.Length);

        foreach (TradeContractNode node in rule.Nodes)
        {
            packet.WriteByte(node.IsFurni ? FurniNode : CoinNode).WriteInteger(node.Amount);

            if (!node.IsFurni)
            {
                continue;
            }

            TradeContractItemType? itemType = node.ItemType;

            packet
                .WriteBoolean(itemType?.IsWallItem ?? false)
                .WriteInteger(itemType?.SpriteId ?? 0)
                .WriteString(itemType?.LegacyPosterId ?? string.Empty);
        }
    }
}
