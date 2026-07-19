using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredmenu;

public class WiredGetVariablesForObjectMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredGetVariablesForObjectMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredGetVariablesForObjectMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        List<(WiredVariableId id, WiredVariableValue value)> variables = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetAllVariablesForBindingAsync(
                new WiredVariableBinding()
                {
                    TargetType = (WiredVariableTargetType)message.SourceType,
                    TargetId = Math.Abs(message.SourceId),
                },
                ct
            )
            .ConfigureAwait(false);

        _ = ctx.SendComposerAsync(
                new WiredVariablesForObjectEventMessageComposer()
                {
                    TargetType = (WiredVariableTargetType)message.SourceType,
                    TargetId = message.SourceId,
                    VariableValues = variables,
                    ConfiguredInWireds = [],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
