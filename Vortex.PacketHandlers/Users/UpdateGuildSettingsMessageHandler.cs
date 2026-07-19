using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Users;
using Vortex.Primitives.Messages.Outgoing.Users;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Users;

public class UpdateGuildSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UpdateGuildSettingsMessage>
{
    private readonly IGrainFactory _grainFactory = grainFactory;

    public async ValueTask HandleAsync(
        UpdateGuildSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.GroupId <= 0)
        {
            return;
        }

        bool ok = await _grainFactory
            .GetGroupGrain(message.GroupId)
            .UpdateSettingsAsync(ctx.PlayerId, message.GuildType, message.RightsLevel, ct)
            .ConfigureAwait(false);

        if (ok)
        {
            await ctx.SendComposerAsync(
                    new GroupDetailsChangedMessageComposer { GroupId = message.GroupId },
                    ct
                )
                .ConfigureAwait(false);
        }
        else
        {
            await ctx.SendComposerAsync(new GuildEditFailedMessageComposer { Reason = 0 }, ct)
                .ConfigureAwait(false);
        }
    }
}
