using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Players;

namespace Turbo.Primitives.Groups.Grains;

/// <summary>
/// One grain per group (key = group id). Owns the group's identity, membership and forum state.
/// </summary>
public interface IGroupGrain : IGrainWithIntegerKey
{
    /// <summary>Detail view for <paramref name="viewer"/>; null if the group does not exist.</summary>
    Task<GroupDetailsSnapshot?> GetDetailsAsync(PlayerId viewer, CancellationToken ct);

    /// <summary>A page of the member list (or pending requests for <c>searchType == 1</c>).</summary>
    Task<GroupMembersPageSnapshot?> GetMembersAsync(
        PlayerId viewer,
        int pageIndex,
        string userNameFilter,
        int searchType,
        CancellationToken ct
    );

    /// <summary>
    /// Attempts to join. Returns null on success (member added or request created), otherwise a
    /// client join-failure reason code.
    /// </summary>
    Task<int?> JoinAsync(PlayerId player, CancellationToken ct);

    // ── Management (owner / admin) ──────────────────────────────────────────────

    /// <summary>Edit-screen data; null if the group is missing or the actor may not manage it.</summary>
    Task<GroupEditInfoSnapshot?> GetEditInfoAsync(PlayerId actor, CancellationToken ct);

    Task<bool> UpdateIdentityAsync(
        PlayerId actor,
        string name,
        string description,
        CancellationToken ct
    );

    Task<bool> UpdateColorsAsync(
        PlayerId actor,
        int primaryColorId,
        int secondaryColorId,
        CancellationToken ct
    );

    Task<bool> UpdateBadgeAsync(
        PlayerId actor,
        IReadOnlyList<int> badgeParts,
        CancellationToken ct
    );

    Task<bool> UpdateSettingsAsync(
        PlayerId actor,
        int guildType,
        int rightsLevel,
        CancellationToken ct
    );

    /// <summary>Dissolves the guild (owner only): detaches the room and soft-deletes the group.</summary>
    Task<bool> DeactivateAsync(PlayerId actor, CancellationToken ct);

    // ── Membership administration ──────────────────────────────────────────────

    /// <summary>Approves a pending request; returns the new member row to broadcast, or null on failure.</summary>
    Task<GroupMemberSnapshot?> ApproveMembershipAsync(
        PlayerId actor,
        int targetPlayerId,
        CancellationToken ct
    );

    Task<bool> RejectMembershipAsync(PlayerId actor, int targetPlayerId, CancellationToken ct);

    /// <summary>Approves every pending request; returns the newly added member rows.</summary>
    Task<List<GroupMemberSnapshot>> ApproveAllMembershipsAsync(
        PlayerId actor,
        CancellationToken ct
    );

    Task<bool> KickMemberAsync(
        PlayerId actor,
        int targetPlayerId,
        bool block,
        CancellationToken ct
    );

    /// <summary>Grants/revokes admin rights; returns the updated member row, or null on failure.</summary>
    Task<GroupMemberSnapshot?> SetAdminRightsAsync(
        PlayerId actor,
        int targetPlayerId,
        bool isAdmin,
        CancellationToken ct
    );

    /// <summary>Count of the target member's furni placed in the guild base room.</summary>
    Task<int> GetMemberFurniCountAsync(int targetPlayerId, CancellationToken ct);
}
