using System;
using System.Collections.Generic;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Data;

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
                    WiredFurniSourceTypeExtensions.FromProtocolId((WiredSourceType)packet.PopInt()),
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
                    ),
                ]);

                userSourceCount--;
            }
        }

        // Everything above is mandatory and always present. What follows is not, and the parser
        // stops rather than reads past the end.
        //
        // The client's composer pushes both trailing lists unconditionally -- its own source ends
        // with `push(p3.length)` then `push(p6.length)`, which is eight bytes even when both are
        // empty. A real 2197 for a six-int action arrives with two bytes left instead of eight
        // (observed 2026-08-22: 64-byte body, parsed cleanly through the user sources, tail `00 00`).
        // So the wire is shorter than the composer reads, and which of the two lists the tail
        // belongs to cannot be told apart when both are empty.
        //
        // Reading them only when the bytes are there loses nothing -- an absent list is an empty
        // list either way -- and turns a message that threw away a whole box's configuration into
        // one that keeps every field the client did send.
        List<string> variableIds = new();

        if (packet.Remaining >= sizeof(int))
        {
            int variableIdCount = packet.PopInt();

            while (variableIdCount > 0 && packet.Remaining > 0)
            {
                variableIds.Add(packet.PopString());

                variableIdCount--;
            }
        }

        List<object> typeSpecifics =
            packet.Remaining > 0 ? ParseSpecifics(packet, GetRequiredTypeSpecifics()) : new();

        List<int> stuffIds2 = new();

        if (packet.Remaining >= sizeof(int))
        {
            int stuffId2Count = packet.PopInt();

            while (stuffId2Count > 0 && packet.Remaining >= sizeof(int))
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
            TypeSpecifics = typeSpecifics,
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
