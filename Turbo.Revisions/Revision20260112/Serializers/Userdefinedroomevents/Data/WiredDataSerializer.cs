using System.Collections.Generic;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Snapshots.Wired;
using Turbo.Primitives.Rooms.Snapshots.Wired.Variables;
using Turbo.Primitives.Rooms.Wired.Variable;

namespace Turbo.Revisions.Revision20260112.Serializers.Userdefinedroomevents.Data;

internal class WiredDataSerializer
{
    public static void Serialize(IServerPacket packet, WiredDataSnapshot snapshot)
    {
        packet.WriteInteger(snapshot.FurniLimit).WriteInteger(snapshot.StuffIds.Count);

        foreach (int stuffId in snapshot.StuffIds)
            packet.WriteInteger(stuffId);

        packet.WriteInteger(snapshot.StuffIds2.Count);

        foreach (int stuffId in snapshot.StuffIds2)
            packet.WriteInteger(stuffId);

        packet
            .WriteInteger(snapshot.StuffTypeId)
            .WriteInteger(snapshot.Id)
            .WriteString(snapshot.StringParam)
            .WriteInteger(snapshot.IntParams.Count);

        foreach (int intParam in snapshot.IntParams)
            packet.WriteInteger(intParam);

        packet.WriteInteger(snapshot.VariableIds.Count);

        foreach (WiredVariableId variableId in snapshot.VariableIds)
            packet.WriteString(variableId.ToString());

        packet.WriteInteger(snapshot.FurniSourceTypes.Count);

        foreach (WiredFurniSourceType[] furniSourceType in snapshot.FurniSourceTypes)
            packet.WriteInteger(
                (int)WiredFurniSourceTypeExtensions.GetProtocolId(furniSourceType[0])
            );

        packet.WriteInteger(snapshot.PlayerSourceTypes.Count);

        foreach (WiredPlayerSourceType[] userSourceType in snapshot.PlayerSourceTypes)
            packet.WriteInteger(
                (int)WiredPlayerSourceTypeExtensions.GetProtocolId(userSourceType[0])
            );

        packet.WriteInteger(snapshot.Code);

        SerializeSpecifics(packet, snapshot.DefinitionSpecifics);

        packet.WriteBoolean(snapshot.AdvancedMode);

        SerializeInputSources(packet, snapshot);

        packet.WriteBoolean(snapshot.AllowWallFurni);

        SerializeSpecifics(packet, snapshot.TypeSpecifics);

        packet.WriteInteger(snapshot.ContextSnapshots.Count);

        foreach (WiredVariableContextSnapshot context in snapshot.ContextSnapshots)
            SerializeWiredContext(packet, context);

        packet.WriteInteger(snapshot.DefaultIntParams.Count);

        foreach (int intParam in snapshot.DefaultIntParams)
            packet.WriteInteger(intParam);
    }

    private static void SerializeSpecifics(IServerPacket packet, List<object> specifics)
    {
        foreach (var specific in specifics)
        {
            if (specific is null)
            {
                continue;
            }

            switch (specific)
            {
                case int intValue:
                    packet.WriteInteger(intValue);
                    break;
                case string stringValue:
                    packet.WriteString(stringValue);
                    break;
                case bool boolValue:
                    packet.WriteBoolean(boolValue);
                    break;
                case byte byteValue:
                    packet.WriteByte(byteValue);
                    break;
                default:
                    break;
            }
        }
    }

    private static void SerializeInputSources(IServerPacket packet, WiredDataSnapshot snapshot)
    {
        packet.WriteInteger(snapshot.AllowedFurniSources.Count);

        foreach (WiredFurniSourceType[] furniSourceList in snapshot.AllowedFurniSources)
        {
            packet.WriteInteger(furniSourceList.Length);

            foreach (WiredFurniSourceType furniSourceId in furniSourceList)
                packet.WriteInteger(
                    (int)WiredFurniSourceTypeExtensions.GetProtocolId(furniSourceId)
                );
        }

        packet.WriteInteger(snapshot.AllowedPlayerSources.Count);

        foreach (WiredPlayerSourceType[] userSourceList in snapshot.AllowedPlayerSources)
        {
            packet.WriteInteger(userSourceList.Length);

            foreach (WiredPlayerSourceType userSourceId in userSourceList)
                packet.WriteInteger(
                    (int)WiredPlayerSourceTypeExtensions.GetProtocolId(userSourceId)
                );
        }

        packet.WriteInteger(snapshot.DefaultFurniSources.Count);

        foreach (WiredFurniSourceType[] type in snapshot.DefaultFurniSources)
            packet.WriteInteger((int)WiredFurniSourceTypeExtensions.GetProtocolId(type[0]));

        packet.WriteInteger(snapshot.DefaultPlayerSources.Count);

        foreach (WiredPlayerSourceType[] type in snapshot.DefaultPlayerSources)
            packet.WriteInteger((int)WiredPlayerSourceTypeExtensions.GetProtocolId(type[0]));
    }

    private static void SerializeWiredContext(
        IServerPacket packet,
        WiredVariableContextSnapshot context
    )
    {
        packet.WriteInteger((int)context.ContextType);

        switch (context)
        {
            case WiredVariableAllInRoomSnapshot allInRoom:
                packet.WriteInteger(allInRoom.AllVariablesHash.Value);
                break;
            case WiredVariableInfoAndHoldersSnapshot infoAndHolders:
                WiredVariableSerializer.Serialize(packet, infoAndHolders.Variable);
                packet.WriteInteger(infoAndHolders.Holders.Count);

                foreach ((int objectId, int value) in infoAndHolders.Holders)
                    packet.WriteInteger(objectId).WriteInteger(value);
                break;
            case WiredVariableInfoAndValueSnapshot infoAndValue:
                WiredVariableSerializer.Serialize(packet, infoAndValue.Variable);
                packet.WriteInteger(infoAndValue.Value);
                break;
            default:
                break;
        }
    }
}
