using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

public class OpenMessageHandler(IGrainFactory grainFactory) : IMessageHandler<OpenMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        OpenMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Id <= 0)
        {
            return;
        }

        WiredDataSnapshot? wiredData = await _grainFactory
            .GetRoomGrain(ctx.RoomId)
            .GetWiredDataSnapshotByFloorItemIdAsync(message.Id, ct)
            .ConfigureAwait(false);

        if (wiredData is null)
        {
            return;
        }

        WiredType wiredType = wiredData.WiredType;
        IComposer? composer = null;

        switch (wiredType)
        {
            case WiredType.Action:
                composer = new WiredFurniActionEventMessageComposer { WiredData = wiredData };
                break;
            case WiredType.Addon:
                composer = new WiredFurniAddonEventMessageComposer { WiredData = wiredData };
                break;
            case WiredType.Condition:
                composer = new WiredFurniConditionEventMessageComposer { WiredData = wiredData };
                break;
            case WiredType.Selector:
                composer = new WiredFurniSelectorEventMessageComposer { WiredData = wiredData };
                break;
            case WiredType.Trigger:
                composer = new WiredFurniTriggerEventMessageComposer { WiredData = wiredData };
                break;
            case WiredType.Variable:
                composer = new WiredFurniVariableEventMessageComposer { WiredData = wiredData };
                break;
        }

        if (composer is null)
        {
            return;
        }

        _ = ctx.SendComposerAsync(composer, ct).ConfigureAwait(false);
    }
}
