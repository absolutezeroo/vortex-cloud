using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Protocol.Messages.Incoming.Room.Furniture;
using Vortex.Protocol.Messages.Outgoing.Room.Furniture;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Room.Furniture;

/// <summary>
/// Answers the context menu of a guild-customized furni with the owning guild's identity, so the
/// client can offer "go to the guild HQ" / "open the guild forum" on the right-click menu.
/// </summary>
public class GetGuildFurniContextMenuInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetGuildFurniContextMenuInfoMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        GetGuildFurniContextMenuInfoMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (message.GuildId <= 0)
        {
            return;
        }

        GroupDetailsSnapshot? details = await _grainFactory
            .GetGroupGrain(message.GuildId)
            .GetDetailsAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (details is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new GuildFurniContextMenuInfoMessageComposer
                {
                    ObjectId = message.ObjectId,
                    GuildId = details.GroupId,
                    GuildName = details.Name,
                    GuildHomeRoomId = details.RoomId,
                    UserIsMember = details.Status == GroupMembershipStatus.Member,
                    GuildHasReadableForum = details.HasForum,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
