using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Users;
using Turbo.Primitives.Messages.Outgoing.Users;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Users;

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
