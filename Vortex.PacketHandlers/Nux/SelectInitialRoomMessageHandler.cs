using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Nux;
using Vortex.Primitives.Messages.Outgoing.Nux;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.PacketHandlers.Nux;

/// <summary>
/// Creates the starter room a new player picked and tells the client which room it got.
/// </summary>
/// <remarks>
/// The client sends a room TYPE, not an id — the room does not exist until this runs. The types are
/// whatever the client was configured to offer (<c>new.user.flow.roomTypes</c>, "10,11,12" by
/// default), so each maps to a room model through <c>Vortex:Nux:RoomModels:&lt;type&gt;</c>; an
/// unmapped type falls back to the configured default, and <see cref="IRoomService"/> falls back
/// again to the first model in the database, so an unknown type still yields a room.
///
/// The client turns a room id greater than zero into an <c>UpdateHomeRoom</c> of its own, then ends
/// the onboarding flow either way — so a failure here must still answer, with id 0.
/// </remarks>
public class SelectInitialRoomMessageHandler(
    IRoomService roomService,
    IGrainFactory grainFactory,
    IConfiguration configuration
) : IMessageHandler<SelectInitialRoomMessage>
{
    private const short StatusOk = 0;
    private const short StatusFailed = 1;

    private readonly IRoomService _roomService = roomService;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IConfiguration _configuration = configuration;

    public async ValueTask HandleAsync(
        SelectInitialRoomMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.PlayerId <= 0)
        {
            await SendFailureAsync(ctx, ct).ConfigureAwait(false);

            return;
        }

        string roomType = message.RoomType ?? string.Empty;
        string defaultModel = _configuration.GetValue("Vortex:Nux:DefaultRoomModel", "model_a")!;
        string modelName = _configuration.GetValue(
            $"Vortex:Nux:RoomModels:{roomType}",
            defaultModel
        )!;
        int maxPlayers = _configuration.GetValue("Vortex:Nux:StarterRoomMaxPlayers", 25);
        int categoryId = _configuration.GetValue("Vortex:Nux:StarterRoomCategoryId", 0);

        string ownerName = await _grainFactory
            .GetPlayerDirectoryGrain()
            .GetPlayerNameAsync(PlayerId.Parse(ctx.PlayerId), ct)
            .ConfigureAwait(false);

        string roomName = string.IsNullOrWhiteSpace(ownerName) ? "My room" : $"{ownerName}'s room";

        (RoomId roomId, string _) = await _roomService
            .CreateRoomAsync(
                roomName,
                string.Empty,
                modelName,
                categoryId,
                maxPlayers,
                RoomTradeModeType.RoomOwnerAndRights,
                PlayerId.Parse(ctx.PlayerId),
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new SelectInitialRoomEventMessageComposer { Status = StatusOk, RoomId = roomId },
                ct
            )
            .ConfigureAwait(false);
    }

    private static async ValueTask SendFailureAsync(MessageContext ctx, CancellationToken ct) =>
        await ctx.SendComposerAsync(
                new SelectInitialRoomEventMessageComposer
                {
                    Status = StatusFailed,
                    RoomId = RoomId.Parse(0),
                },
                ct
            )
            .ConfigureAwait(false);
}
