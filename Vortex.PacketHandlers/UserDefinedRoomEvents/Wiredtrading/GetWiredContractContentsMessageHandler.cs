using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Action;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading.Contracts;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents.Wiredtrading;

/// <summary>
/// The contract editor asking what it is editing.
/// </summary>
/// <remarks>
/// Sent straight back after the server pushes "open contract N", and also why the permission is
/// checked again in the room: a client can send this without ever having clicked the furni.
/// </remarks>
public class GetWiredContractContentsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetWiredContractContentsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetWiredContractContentsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredContractSnapshot? contract = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetWiredContractAsync(
                ActionContext.CreateForPlayer(ctx.PlayerId, ctx.RoomId),
                message.ContractId,
                ct
            )
            .ConfigureAwait(false);

        if (contract is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new WiredContractContentsMessageComposer { Contract = contract },
                ct
            )
            .ConfigureAwait(false);
    }
}
