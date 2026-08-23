using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The contract editor's save button.
/// </summary>
/// <remarks>
/// Answered with what was stored rather than with an acknowledgement, so the window redraws from
/// the server's reading of the save — a field the server declined to keep leaves the screen instead
/// of lingering there until the next open.
/// </remarks>
public class SaveWiredContractMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SaveWiredContractMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        SaveWiredContractMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredContractSnapshot? saved = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .SaveWiredContractAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.Contract,
                ct
            )
            .ConfigureAwait(false);

        if (saved is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredContractContentsMessageComposer { Contract = saved },
                ct
            )
            .ConfigureAwait(false);
    }
}
