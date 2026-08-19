using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Messages.Outgoing.Userclassification;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Server.Grains;

namespace Vortex.PacketHandlers.UserClassification;

/// <summary>
/// Shared tail of the two <c>:uc</c> commands. They differ only in which set of players they look
/// at; the gate, the tunables and the reply are the same.
/// </summary>
internal static class UserClassificationDispatch
{
    public static async Task RespondAsync(
        IGrainFactory grainFactory,
        IPermissionService permissionService,
        IUserClassificationService classifications,
        MessageContext ctx,
        IReadOnlyCollection<int> candidateIds,
        string classification,
        CancellationToken ct
    )
    {
        // The client gates the command on hasSecurity(4); server-side that is the staff room-wide
        // capability. Checked here and not in the client's word for it.
        PermissionSet permissions = await permissionService
            .ResolveForPlayerAsync(ctx.PlayerId, ct)
            .ConfigureAwait(false);

        if (!permissions.HasAny(Capabilities.Room.ModerateAny))
        {
            return;
        }

        if (candidateIds.Count == 0)
        {
            return;
        }

        int newUserWindowDays = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(
                ModerationConfig.NewUserClassificationDaysKey,
                ModerationConfig.NewUserClassificationDaysDefault
            )
            .ConfigureAwait(false);

        ImmutableArray<UserClassificationEntry> entries = await classifications
            .ClassifyAsync(candidateIds, classification, newUserWindowDays, ct)
            .ConfigureAwait(false);

        // Sent even when empty: the client opens its window on this message, and a moderator who
        // typed the command needs to see "nobody matched" rather than nothing happening at all.
        await ctx.SendComposerAsync(new UserClassificationMessageComposer { Entries = entries }, ct)
            .ConfigureAwait(false);
    }
}
