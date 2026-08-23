using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;
using Vortex.Primitives.Server.Grains;
using Vortex.Protocol.Messages.Incoming.Userclassification;

namespace Vortex.PacketHandlers.UserClassification;

/// <summary>
/// The staff <c>:uc hotel &lt;classification&gt;</c> command: the same sweep as the room-scoped one
/// but over everybody online.
/// </summary>
public class PeerUsersClassificationMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService,
    IUserClassificationService classifications,
    ISessionGateway sessionGateway
) : IMessageHandler<PeerUsersClassificationMessage>
{
    public async ValueTask HandleAsync(
        PeerUsersClassificationMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // Capped: unlike the room-scoped form this is bounded only by how busy the hotel is, and
        // the client's list window is not something a moderator reads ten thousand rows of.
        int limit = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(
                ModerationConfig.UserClassificationHotelLimitKey,
                ModerationConfig.UserClassificationHotelLimitDefault
            )
            .ConfigureAwait(false);

        int[] playerIds =
        [
            .. sessionGateway
                .GetOnlinePlayerIds()
                .Select(id => id.Value)
                .Where(id => id > 0)
                .Take(limit),
        ];

        await UserClassificationDispatch
            .RespondAsync(
                grainFactory,
                permissionService,
                classifications,
                ctx,
                playerIds,
                message.Classification,
                ct
            )
            .ConfigureAwait(false);
    }
}
