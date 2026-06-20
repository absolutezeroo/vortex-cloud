using System;
using System.Collections.Generic;
using Turbo.Primitives.Messages.Incoming.Userdefinedroomevents;
using Turbo.Primitives.Packets;
using Turbo.Revisions.Revision20260112.Parsers.UserDefinedRoomEvents.Data;

namespace Turbo.Revisions.Revision20260112.Parsers.UserDefinedRoomEvents;

internal class UpdateActionMessageParser : UpdateWiredDataParser, IParser
{
    public override List<object> GetRequiredDefinitionSpecifics() => [1];

    public override Type UpdateMessageType => typeof(UpdateActionMessage);
}
