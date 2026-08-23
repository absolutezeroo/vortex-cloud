using System.Collections.Immutable;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;

namespace Vortex.Revisions.Revision20260701.Serializers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The requirement reads itself first and its length varies with its type, so the three
/// presentation fields can only follow it — the client's parser has the same ordering constraint
/// and says so.
/// </summary>
internal class WiredTradeInitiateMessageComposerSerializer(int header)
    : AbstractSerializer<WiredTradeInitiateMessageComposer>(header)
{
    protected override void Serialize(
        IServerPacket packet,
        WiredTradeInitiateMessageComposer message
    )
    {
        packet
            .WriteInteger(message.RequirementType)
            .WriteString(message.YouGetText)
            .WriteString(message.LayoutType);

        // Only a custom contract carries rules, and the client reads them off the same test — send
        // one under any other type and everything after it is read at the wrong offset.
        if (message.RequirementType == CustomRequirementType && message.Contract is not null)
        {
            WriteContract(packet, message.Contract);
        }

        packet
            .WriteBoolean(message.ShowRequirementsImmediate)
            .WriteBoolean(message.OverridePreviousTrade)
            .WriteInteger(message.TimeoutSeconds);
    }

    /// <summary>The one requirement type whose payload is followed by a rules block.</summary>
    private const int CustomRequirementType = 4;

    /// <summary>Coin terms go out as 0, furni terms as 1 — a byte, not an int.</summary>
    private const byte CoinNode = 0;

    private const byte FurniNode = 1;

    private static void WriteContract(IServerPacket packet, TradeContract contract)
    {
        ImmutableArray<TradeContractRule>? giveRules = contract.YouGiveRules;

        packet.WriteBoolean(giveRules is not null);

        if (giveRules is not null)
        {
            packet.WriteInteger(giveRules.Value.Length);

            foreach (TradeContractRule rule in giveRules.Value)
            {
                WriteRule(packet, rule);
            }
        }

        packet.WriteBoolean(contract.YouGetRule is not null);

        if (contract.YouGetRule is not null)
        {
            WriteRule(packet, contract.YouGetRule);
        }

        packet.WriteInteger(contract.Mode);

        // The mode decides which single trailing int follows it, or none at all.
        if (contract.Mode == 1)
        {
            packet.WriteInteger(contract.Multiplier);
        }
        else if (contract.Mode == 2)
        {
            packet.WriteInteger(contract.AutoMultiplierMax);
        }
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
