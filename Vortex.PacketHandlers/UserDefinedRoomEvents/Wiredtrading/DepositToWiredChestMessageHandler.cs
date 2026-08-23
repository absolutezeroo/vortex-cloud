using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;

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

        if (start == WiredDepositStart.Refused)
        {
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
