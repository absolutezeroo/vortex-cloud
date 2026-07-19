using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class CreateGuildMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CreateGuildMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        CreateGuildMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        int? groupId = await _grainFactory
            .GetGroupDirectoryGrain()
            .CreateGroupAsync(
                ctx.PlayerId,
                message.Name,
                message.Description,
                message.PrimaryColorId,
                message.SecondaryColorId,
                message.BaseRoomId,
                message.BadgeParts,
                ct
            )
            .ConfigureAwait(false);

        // Validation failed (room not owned / already a guild base) — nothing to confirm.
        if (groupId is null)
        {
            return;
        }

        await ctx.SendComposerAsync(
                new GuildCreatedMessageComposer
                {
                    BaseRoomId = message.BaseRoomId,
                    GroupId = groupId.Value,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
