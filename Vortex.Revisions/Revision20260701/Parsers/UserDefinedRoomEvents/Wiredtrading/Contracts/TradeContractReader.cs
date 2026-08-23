using System.Collections.Generic;
using System.Collections.Immutable;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Wiredtrading.Contracts;

/// <summary>
/// Reading a contract's terms off the wire.
/// </summary>
/// <remarks>
/// The client writes them with the same class that reads them back, so this is the exact mirror of
/// the serializer — announced sides, counted rules, counted nodes, and an item kind only behind a
/// furni term. One field read out of turn takes the rest of the message with it.
/// </remarks>
internal static class TradeContractReader
{
    /// <summary>A furni term is 1 on the wire; a coin term is 0.</summary>
    private const byte FurniNode = 1;

    public static (ImmutableArray<TradeContractRule>? Give, TradeContractRule? Get) ReadDefinition(
        IClientPacket packet
    )
    {
        ImmutableArray<TradeContractRule>? give = null;

        if (packet.PopBoolean())
        {
            int count = packet.PopInt();
            List<TradeContractRule> rules = [];

            for (int index = 0; index < count; index++)
            {
                rules.Add(ReadRule(packet));
            }

            give = [.. rules];
        }

        TradeContractRule? get = packet.PopBoolean() ? ReadRule(packet) : null;

        return (give, get);
    }

    private static TradeContractRule ReadRule(IClientPacket packet)
    {
        int count = packet.PopInt();
        List<TradeContractNode> nodes = [];

        for (int index = 0; index < count; index++)
        {
            bool isFurni = packet.PopByte() == FurniNode;
            int amount = packet.PopInt();

            nodes.Add(
                new TradeContractNode
                {
                    IsFurni = isFurni,
                    Amount = amount,
                    ItemType = isFurni
                        ? new TradeContractItemType
                        {
                            IsWallItem = packet.PopBoolean(),
                            SpriteId = packet.PopInt(),
                            LegacyPosterId = packet.PopString(),
                        }
                        : null,
                }
            );
        }

        return new TradeContractRule { Nodes = [.. nodes] };
    }
}
