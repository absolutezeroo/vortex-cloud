using System;
using System.Collections.Generic;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Packets;
using Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents.Data;

namespace Vortex.Revisions.Revision20260701.Parsers.UserDefinedRoomEvents;

internal class UpdateSelectorMessageParser : UpdateWiredDataParser, IParser
{
    public override List<object> GetRequiredDefinitionSpecifics() => [true, true];

    public override Type UpdateMessageType => typeof(UpdateSelectorMessage);
}
