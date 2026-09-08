using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Dashboard.API.Operations.Hotel.Contracts;
using Vortex.Primitives.Groups.Enums;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Dashboard.API.Operations.Hotel;

/// <summary>
/// Acting on a guild and its forum from outside: the public content that gets reported, and the
/// membership decisions nobody in the guild is willing to make.
/// </summary>
/// <remarks>
/// <para>
/// Every write goes through <c>IGroupGrain</c> / <c>IGroupForumGrain</c> and none of them touches a
/// table. That is not the usual reason — neither grain caches anything, so a direct write would be
/// live — but the grains publish the domain event and, for membership, notify the guild's base room
/// so the roster that carries build rights is dropped. A dashboard write that skipped that would
/// leave an ex-member building in a guild they are no longer in, and would be invisible until
/// somebody noticed.
/// </para>
/// <para>
/// It is audited under <see cref="AuditCategory.Moderation"/> rather than Staff: these are actions
/// taken on someone's content or standing, and they belong in the same trail as a mute or a room
/// kick when an operator is later asked to justify one.
/// </para>
/// </remarks>
internal sealed class GuildOperations(
    OperationRunner runner,
    IGrainFactory grainFactory,
    StaffActorAccount staffActor
)
{
    private readonly OperationRunner _runner = runner;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly StaffActorAccount _staffActor = staffActor;

    public Task<OperationResult> ModerateThreadAsync(
        ModerateForumThreadRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
            "ops.guild.forum.thread",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.GuildId,
                request.ThreadId,
                Action = request.Action.ToString(),
            },
            work: async c =>
            {
                PlayerId staff = await _staffActor.PlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetGroupForumGrain(request.GuildId)
                    .StaffModerateThreadAsync(staff.Value, request.ThreadId, request.Action, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("thread_not_found");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> ModeratePostAsync(
        ModerateForumPostRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
            "ops.guild.forum.post",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.GuildId,
                request.PostId,
                Action = request.Action.ToString(),
            },
            work: async c =>
            {
                PlayerId staff = await _staffActor.PlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetGroupForumGrain(request.GuildId)
                    .StaffModeratePostAsync(staff.Value, request.PostId, request.Action, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("post_not_found");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> MemberActionAsync(
        GuildMemberActionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
            "ops.guild.member." + ActionSuffix(request.Action),
            actor,
            request.Reason,
            // The audit names the player: a guild kick is something done to somebody, and looking up
            // what was done to a player is how the trail is read.
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.GuildId, Action = request.Action.ToString() },
            work: async c =>
            {
                PlayerId staff = await _staffActor.PlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetGroupGrain(request.GuildId)
                    .StaffMemberActionAsync(staff.Value, request.PlayerId, request.Action, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    // One code for all five: the grain answers false when there was nothing to do,
                    // and "the request you were answering is already gone" is the same news to the
                    // operator whichever action they picked.
                    throw new InvalidOperationException("guild_action_rejected");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    public Task<OperationResult> DeleteGuildAsync(
        DeleteGuildRequest request,
        string actor,
        CancellationToken ct
    ) =>
        _runner.ExecuteAsync(
            "ops.guild.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.GuildId },
            work: async c =>
            {
                PlayerId staff = await _staffActor.PlayerIdAsync(c).ConfigureAwait(false);

                bool ok = await _grainFactory
                    .GetGroupGrain(request.GuildId)
                    .StaffDeactivateAsync(staff.Value, c)
                    .ConfigureAwait(false);

                if (!ok)
                {
                    throw new InvalidOperationException("guild_not_found");
                }
            },
            ct,
            AuditCategory.Moderation
        );

    /// <summary>The action's own name in the audit row, so the trail is greppable per action rather
    /// than one bucket that has to be opened to find out what happened.</summary>
    private static string ActionSuffix(GroupStaffAction action) =>
        action switch
        {
            GroupStaffAction.ApproveRequest => "approve",
            GroupStaffAction.RejectRequest => "reject",
            GroupStaffAction.Kick => "kick",
            GroupStaffAction.KickAndBlock => "kick_block",
            _ => "unban",
        };
}
