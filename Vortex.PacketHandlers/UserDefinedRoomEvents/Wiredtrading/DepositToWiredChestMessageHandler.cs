using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The chest screen's deposit button.
/// </summary>
/// <remarks>
/// The message carries the chest id alone, and what follows it is not a dialog of its own: the
/// client answers by waiting for the inventory's wired-trade screen. That is readable off the
/// client rather than guessed — <c>WiredTradingModel</c> is fully ported and sends exactly the
/// three messages this trade needs, and nothing else on the client can move an item into a chest.
/// <para>
/// **Furniture only.** A coin chest is refused here, because the player has no way to name an
/// amount: the only message the trade screen sends carries item ids, and the credits on each side
/// are dictated by the server in the table update. What the official server does for that half
/// cannot be read off the client, so it stays unknown rather than invented.
/// </para>
/// </remarks>
public class DepositToWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<DepositToWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    /// <summary>How long the client gives the player before closing the screen itself.</summary>
    private const int DepositTimeoutSeconds = 300;

    /// <summary>Any tradeable furni qualifies — see the composer's own note on the four types.</summary>
    private const int RequirementAnyFurni = 2;

    /// <summary>
    /// "No chests or locked chests" in the client's own failure table
    /// (<c>wired_transactions.notification.fail.15</c>).
    /// </summary>
    private const int LockedChestFailure = 15;

    public async ValueTask HandleAsync(
        DepositToWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredDepositStart start = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .StartWiredChestDepositAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                ct
            )
            .ConfigureAwait(false);

        if (start == WiredDepositStart.RefusedLocked)
        {
            // The one refusal this client can say out loud. WiredTradingView.alertTradeCancelled
            // feeds the code to `wired_transactions.notification.fail.<code>`, and 15 is that
            // table's "No chests or locked chests"; the view closes the screen and shows the popup.
            // Code 0 is deliberately silent there (WiredTradingModel's own "player closed it"
            // sentinel), which is why refusing with 0 would be no better than saying nothing.
            await ctx.SendComposerAsync(
                    new WiredTradeCancelledMessageComposer
                    {
                        TransactionFailureTypeId = LockedChestFailure,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        if (start == WiredDepositStart.Refused)
        {
            // Every other refusal -- not a chest, a coin chest, no rights on the room -- stays
            // silent. What the official server answers is not known (the spec's scenarios are all
            // `expected: unknown`) and none of the client's other failure texts is close enough to
            // pick without guessing.
            return;
        }

        await ctx.SendComposerAsync(
                new WiredTradeInitiateMessageComposer
                {
                    RequirementType = RequirementAnyFurni,
                    // Nothing comes back out of a deposit, which is what makes the client read the
                    // contract as payment-only.
                    YouGetText = string.Empty,
                    LayoutType = string.Empty,
                    ShowRequirementsImmediate = false,
                    OverridePreviousTrade = start == WiredDepositStart.Replaced,
                    TimeoutSeconds = DepositTimeoutSeconds,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
