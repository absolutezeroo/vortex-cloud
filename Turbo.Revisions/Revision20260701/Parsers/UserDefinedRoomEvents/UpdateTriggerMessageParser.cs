using System;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class UpdateTriggerMessageParser : UpdateWiredDataParser, IParser
{
    public override Type UpdateMessageType => typeof(UpdateTriggerMessage);
}
