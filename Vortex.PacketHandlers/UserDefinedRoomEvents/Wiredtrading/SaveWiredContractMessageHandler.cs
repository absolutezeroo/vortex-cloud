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
            // The editor stays open on a refusal and says why; "invalid_rules" is the one reason
            // the texts carry — "Invalid or empty requirements".
            await ctx.SendComposerAsync(
                    new WiredContractUpdateResultMessageComposer
                    {
                        ContractId = message.Contract.ContractId,
                        IsSuccess = false,
                        FailCode = InvalidRules,
                    },
                    ct
                )
                .ConfigureAwait(false);

            return;
        }

        await ctx.SendComposerAsync(
                new WiredContractContentsMessageComposer { Contract = saved },
                ct
            )
            .ConfigureAwait(false);

        // And this is what closes it. The contents reply redraws the window; only the result
        // dismisses it, so a save answered with contents alone leaves the editor open on a contract
        // that was already stored.
        await ctx.SendComposerAsync(
                new WiredContractUpdateResultMessageComposer
                {
                    ContractId = saved.ContractId,
                    IsSuccess = true,
                    FailCode = string.Empty,
                },
                ct
            )
            .ConfigureAwait(false);
    }

    /// <summary>The one refusal the client has a text for.</summary>
    private const string InvalidRules = "invalid_rules";
}
