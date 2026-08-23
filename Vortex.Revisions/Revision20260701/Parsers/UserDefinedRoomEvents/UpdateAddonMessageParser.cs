using System;
using Vortex.Primitives.Packets;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class UpdateAddonMessageParser : UpdateWiredDataParser, IParser
{
    public override Type UpdateMessageType => typeof(UpdateAddonMessage);
}
