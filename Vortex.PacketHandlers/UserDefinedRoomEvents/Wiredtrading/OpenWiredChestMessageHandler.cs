using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The client asking what a chest holds, right after being told to open it.
/// </summary>
/// <remarks>
/// The answer carries <c>isUpdate = false</c>, which is what actually opens the screen on the
/// client's side; a true there would leave it closed and showing nothing.
/// </remarks>
public class OpenWiredChestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<OpenWiredChestMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        OpenWiredChestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredChestSnapshot? chest = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .OpenWiredChestAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ChestId,
                ct
            )
            .ConfigureAwait(false);

        if (chest is null)
        {
            return;
        }

        if (chest.IsCoinChest)
        {
            await ctx.SendComposerAsync(
                    new WiredChestCoinsMessageComposer
                    {
                        ChestId = chest.ChestId,
                        Coins = chest.Credits,
                        IsUpdate = false,
                    },
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}
