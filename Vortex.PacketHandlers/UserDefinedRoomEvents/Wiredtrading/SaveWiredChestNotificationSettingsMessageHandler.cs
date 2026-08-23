using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// When and about what a chest notifies its owner.
/// </summary>
public class SaveWiredChestNotificationSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveWiredChestNotificationSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SaveWiredChestNotificationSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .SaveWiredChestNotificationSettingsAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.NotificationMode,
                message.NotifyWhenFull,
                message.NotifyOnDonation,
                message.NotifyOnWithdraw,
                message.NotifyWhenEmpty,
                message.NotifyOnAnyWiredTransaction,
                ct
            )
            .ConfigureAwait(false);

        // The screen closes on this, not on the save itself — without it the dialog sits there
        // after a save that worked.
        await ctx.SendComposerAsync(
                new WiredChestUpdateSuccessMessageComposer
                {
                    ChestId = message.ChestId,
                    IsNotificationPreferences = true,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
