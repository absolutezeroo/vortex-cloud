using System;
using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Packets;
using Turbo.Primitives.Rooms.Enums.Wired;

namespace Turbo.Revisions.Revision20260112.Parsers.Userdefinedroomevents.Data;

internal abstract class UpdateWiredDataParser : IParser
{
    public virtual Type UpdateMessageType => typeof(UpdateWiredMessage);

    public IMessageEvent Parse(IClientPacket packet)
    {
        int id = packet.PopInt();

        List<int> intParams = new();
        int intParamCount = packet.PopInt();

        if (intParamCount > 0)
        {
            while (intParamCount > 0)
            {
                intParams.Add(packet.PopInt());

                intParamCount--;
            }
        }

        string stringParam = packet.PopString();

        List<int> stuffIds = new();
        int stuffIdCount = packet.PopInt();

        if (stuffIdCount > 0)
        {
            while (stuffIdCount > 0)
            {
                stuffIds.Add(packet.PopInt());

                stuffIdCount--;
            }
        }

        List<object> definitionSpecifics = ParseSpecifics(packet, GetRequiredDefinitionSpecifics());

        List<WiredFurniSourceType[]> furniSources = new();
        int furniSourceCount = packet.PopInt();

        if (furniSourceCount > 0)
        {
            while (furniSourceCount > 0)
            {
                furniSources.Add([
                    WiredFurniSourceTypeExtensions.FromProtocolId((WiredSourceType)packet.PopInt())
                ]);

                furniSourceCount--;
            }
        }

        List<WiredPlayerSourceType[]> userSources = new();
        int userSourceCount = packet.PopInt();

        if (userSourceCount > 0)
        {
            while (userSourceCount > 0)
            {
                userSources.Add([
                    WiredPlayerSourceTypeExtensions.FromProtocolId(
                        (WiredSourceType)packet.PopInt()
                    )
                ]);

                userSourceCount--;
            }
        }

        List<string> variableIds = new();
        int variableIdCount = packet.PopInt();

        if (variableIdCount > 0)
        {
            while (variableIdCount > 0)
            {
                variableIds.Add(packet.PopString());

                variableIdCount--;
            }
        }

        List<object> typeSpecifics = ParseSpecifics(packet, GetRequiredTypeSpecifics());

        List<int> stuffIds2 = new();
        int stuffId2Count = packet.PopInt();

        if (stuffId2Count > 0)
        {
            while (stuffId2Count > 0)
            {
                stuffIds2.Add(packet.PopInt());

                stuffId2Count--;
            }
        }

        UpdateWiredMessage message = (UpdateWiredMessage)
            Activator.CreateInstance(UpdateMessageType)!;

        return message with
        {
            Id = id,
            IntParams = intParams,
            StringParam = stringParam,
            StuffIds = stuffIds,
            StuffIds2 = stuffIds2,
            DefinitionSpecifics = definitionSpecifics,
            FurniSources = furniSources,
            PlayerSources = userSources,
            VariableIds = variableIds,
            TypeSpecifics = typeSpecifics
        };
    }

    public virtual List<object> GetRequiredDefinitionSpecifics()
    {
        return [];
    }

    public virtual List<object> GetRequiredTypeSpecifics()
    {
        return [];
    }

    private List<object> ParseSpecifics(IClientPacket packet, List<object> requiredSpecifics)
    {
        List<object> specifics = new();

        foreach (object specific in requiredSpecifics)
        {
            if (specific is int)
            {
                specifics.Add(packet.PopInt());
            }
            else if (specific is string)
            {
                specifics.Add(packet.PopString());
            }
            else if (specific is bool)
            {
                specifics.Add(packet.PopBoolean());
            }
            else if (specific is byte)
            {
                specifics.Add(packet.PopByte());
            }
        }

        return specifics;
    }
}
