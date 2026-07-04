using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Turbo.Events.Registry;
using Turbo.Players.Configuration;
using Turbo.Primitives.Events;

namespace Turbo.Players.Events;

/// <summary>
///     Rejects guild creation with an empty/whitespace-only or too-long name. Neither
///     <see cref="GroupDirectoryGrain" /> nor <c>CreateGuildMessageHandler</c> validated
///     <c>Name</c> before this — <c>GroupEntity.Name</c> has no length constraint either — so a
///     player could create a guild with an empty or arbitrarily long name.
/// </summary>
internal sealed class GroupNameValidationBehavior(IOptions<GroupConfig> groupConfig)
    : IEventBehavior<GroupCreatingEvent>
{
    private readonly GroupConfig _groupConfig = groupConfig.Value;

    public async ValueTask InvokeAsync(
        GroupCreatingEvent env,
        EventContext ctx,
        Func<ValueTask> next,
        CancellationToken ct
    )
    {
        string name = env.GroupName?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            ctx.Cancel = true;
            ctx.CancelReason = "empty_name";
        }
        else if (name.Length > _groupConfig.MaxNameLength)
        {
            ctx.Cancel = true;
            ctx.CancelReason = "name_too_long";
        }

        await next().ConfigureAwait(false);
    }
}
