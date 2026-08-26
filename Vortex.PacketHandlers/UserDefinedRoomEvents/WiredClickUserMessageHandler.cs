using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents;

namespace Vortex.PacketHandlers.UserDefinedRoomEvents;

/// <summary>
/// Answers the click the client suppressed the context menu for.
/// </summary>
/// <remarks>
/// This deliberately raises no room event. The client sends this <em>in addition to</em> the ordinary
/// <c>ClickCharacter</c>, which already publishes <c>PlayerClickedPlayerEvent</c> and already drives
/// the click-user trigger; publishing here as well would fire every such trigger twice per click.
/// What Habbo's own server does on the two messages is not known from the client alone and stays
/// unknown — what is known is that firing once is the behaviour this emulator already has and tests.
/// </remarks>
public class WiredClickUserMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<WiredClickUserMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        WiredClickUserMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0)
        {
            return;
        }

        WiredClickUserSnapshot state = await _grainFactory
            .GetRoomWired(ctx.RoomId)
            .GetClickUserStateAsync(ct)
            .ConfigureAwait(false);

        // Always answered, including when no box is present. The client has already hidden the menu
        // by the time it asks; staying silent leaves the info stand without its buttons for good.
        await ctx.SendComposerAsync(
                new WiredClickUserResponseMessageComposer
                {
                    Index = message.ObjectId,
                    OpenMenu = !state.BlocksMenu,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
