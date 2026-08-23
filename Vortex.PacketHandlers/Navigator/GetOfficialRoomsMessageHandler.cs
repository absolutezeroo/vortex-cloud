using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Messages.Registry;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Orleans.Snapshots.Navigator;
using Vortex.Protocol.Messages.Incoming.Navigator;
using Vortex.Protocol.Messages.Outgoing.Navigator;

namespace Vortex.PacketHandlers.Navigator;

/// <summary>
/// Answers the public-rooms view with the composer the client actually listens for. It used to
/// reply with a generic new-navigator search block instead, so the old navigator's public-rooms
/// panel never received anything it could render.
/// </summary>
public class GetOfficialRoomsMessageHandler(INavigatorService navigatorService)
    : IMessageHandler<GetOfficialRoomsMessage>
{
    private readonly INavigatorService _navigatorService = navigatorService;

    public async ValueTask HandleAsync(
        GetOfficialRoomsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ImmutableArray<OfficialRoomEntrySnapshot> entries = await _navigatorService
            .GetOfficialRoomEntriesAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new OfficialRoomsMessageComposer
                {
                    Entries = entries,
                    // No hotel-wide promo banner and no promoted-room groups exist as data; both are
                    // sent empty rather than filled with invented entries.
                    AdRoom = null,
                    PromotedRooms = [],
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
