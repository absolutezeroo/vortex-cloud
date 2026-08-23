using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Permissions;
using Vortex.Protocol.Messages.Incoming.Moderator;

namespace Vortex.PacketHandlers.Moderator;

/// <summary>
/// Remembers where the moderator left their mod-tool window. Fires on every move and resize, so it
/// stays a single upsert with no response packet — the client is not waiting on an acknowledgement.
/// </summary>
public class ModToolPreferencesMessageHandler(
    IGrainFactory grainFactory,
    IPermissionService permissionService
) : IMessageHandler<ModToolPreferencesMessage>
{
    public async ValueTask HandleAsync(
        ModToolPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        // A non-staff client has no mod tool to position; refusing here keeps the table to actual
        // staff rows rather than trusting a packet anyone could forge.
        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Moderation.Cfh, Capabilities.Room.ModerateAny))
        {
            return;
        }

        // A collapsed or off-screen rectangle would leave the tool unusable at the next login, and
        // the client has no way to reset it — so a degenerate size is simply not stored.
        if (message.WindowWidth <= 0 || message.WindowHeight <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetModToolPreferencesAsync(
                new PlayerModToolPreferencesSnapshot
                {
                    WindowX = message.WindowX,
                    WindowY = message.WindowY,
                    WindowWidth = message.WindowWidth,
                    WindowHeight = message.WindowHeight,
                    IsSet = true,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}
