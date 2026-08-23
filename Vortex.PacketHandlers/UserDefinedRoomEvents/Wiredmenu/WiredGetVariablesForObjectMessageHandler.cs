using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredmenu;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredmenu;

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
            .GetRoomFurni(ctx.RoomId)
            .GetAllVariablesForBindingAsync(
                new WiredVariableBinding()
                {
                    TargetType = (WiredVariableTargetType)message.SourceType,
                    TargetId = Math.Abs(message.SourceId),
                },
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
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
