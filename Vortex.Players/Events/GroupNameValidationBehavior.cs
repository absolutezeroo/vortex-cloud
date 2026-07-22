using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Players.Configuration;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Players.Events;

/// <summary>
///     Rejects guild creation with an empty/whitespace-only or too-long name. Neither
///     <see cref="GroupDirectoryGrain" /> nor <c>CreateGuildMessageHandler</c> validated
///     <c>Name</c> before this — <c>GroupEntity.Name</c> has no length constraint either — so a
///     player could create a guild with an empty or arbitrarily long name.
/// </summary>
internal sealed class GroupNameValidationBehavior(IGrainFactory grainFactory)
    : IEventBehavior<GroupCreatingEvent>
{
    public async ValueTask InvokeAsync(
        GroupCreatingEvent env,
        EventContext ctx,
        Func<ValueTask> next,
        CancellationToken ct
    )
    {
        int maxNameLength = await grainFactory
            .GetServerConfigGrain()
            .GetIntAsync(GroupConfig.MaxNameLengthKey, GroupConfig.MaxNameLengthDefault)
            .ConfigureAwait(false);

        string name = env.GroupName?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            ctx.Cancel = true;
            ctx.CancelReason = "empty_name";
        }
        else if (name.Length > maxNameLength)
        {
            ctx.Cancel = true;
            ctx.CancelReason = "name_too_long";
        }

        await next().ConfigureAwait(false);
    }
}
