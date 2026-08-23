using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The chest upgrade dialog's buy button.
/// </summary>
/// <remarks>
/// Registered so the message is read rather than logged as unknown, and refused inside the room —
/// see <c>UpgradeWiredChestAsync</c> for why an upgrade with no price is not granted. The client
/// asks for nothing back, so a refusal leaves the dialog as it was.
/// </remarks>
public class UpgradeWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpgradeWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpgradeWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        bool granted = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .UpgradeWiredChestAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                message.UpgradeType,
                ct
            )
            .ConfigureAwait(false);

        // The dialog closes on the answer and reads a non-zero code as
        // `wiredchests.upgrade.result.error.N`. One is "Feature disabled", which is exactly what a
        // hotel with no upgrade prices should be telling the player.
        await ctx.SendComposerAsync(
                new WiredChestUpgradeResultMessageComposer
                {
                    ChestId = message.ChestId,
                    ResultCode = granted ? UpgradeGranted : UpgradeDisabled,
                },
                ct
            )
            .ConfigureAwait(false);
    }

    private const int UpgradeGranted = 0;

    private const int UpgradeDisabled = 1;
}
