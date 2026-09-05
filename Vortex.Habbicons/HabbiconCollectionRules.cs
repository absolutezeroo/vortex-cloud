using System.Collections.Generic;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;

namespace Vortex.Habbicons;

/// <summary>
/// What completing a collection means, and what a player may do about it. Pure functions over
/// ownership: no database, no grain, no clock beyond what is handed in — which is what makes every
/// one of these directly testable, and what keeps the answer the same however it is asked.
/// </summary>
internal static class HabbiconCollectionRules
{
    /// <summary>
    /// Whether the player owns every ordinary entry of a collection.
    /// </summary>
    /// <remarks>
    /// Derived from ownership every time it is asked, never stored. An <c>is_complete</c> column
    /// would be a second copy of a fact the ownership rows already carry, and the first revoked
    /// Habbicon would make the two disagree with nothing to say which was right.
    /// </remarks>
    public static bool IsComplete(
        HabbiconCollectionSnapshot collection,
        IReadOnlyDictionary<int, HabbiconState> owned
    )
    {
        if (collection.Entries.IsDefaultOrEmpty)
        {
            // A set with no entries is not "complete", it is unfinished content. Saying otherwise
            // would hand out its bonus to everyone the moment it was created.
            return false;
        }

        foreach (HabbiconDefinitionSnapshot entry in collection.Entries)
        {
            if (!owned.ContainsKey(entry.HabbiconId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// What state the collection's bonus Habbicon should be in, given the player's ownership.
    /// </summary>
    /// <remarks>
    /// The one place the <see cref="HabbiconState.Claimable"/> transition is decided. Note that an
    /// already-owned bonus is returned untouched: a player who claimed it and then lost an entry
    /// (an operator revoke, a definition edit) keeps what they were given. Taking a reward back
    /// because the content changed underneath them is the kind of correctness nobody wants.
    /// </remarks>
    public static HabbiconState ResolveRewardState(
        HabbiconCollectionSnapshot collection,
        IReadOnlyDictionary<int, HabbiconState> owned
    )
    {
        if (collection.RewardHabbicon is null)
        {
            return HabbiconState.NotOwned;
        }

        if (owned.TryGetValue(collection.RewardHabbicon.HabbiconId, out HabbiconState state))
        {
            return state;
        }

        return IsComplete(collection, owned) ? HabbiconState.Claimable : HabbiconState.NotOwned;
    }

    /// <summary>
    /// Whether the bonus of <paramref name="collection"/> can be claimed right now — complete, and
    /// not already taken.
    /// </summary>
    public static bool CanClaimReward(
        HabbiconCollectionSnapshot collection,
        IReadOnlyDictionary<int, HabbiconState> owned
    ) => ResolveRewardState(collection, owned) == HabbiconState.Claimable;

    /// <summary>
    /// The entries of <paramref name="collection"/> the player is still missing, which is what a
    /// whole-set purchase actually buys. Never includes the bonus: that is claimed, not bought.
    /// </summary>
    public static List<HabbiconDefinitionSnapshot> MissingEntries(
        HabbiconCollectionSnapshot collection,
        IReadOnlyDictionary<int, HabbiconState> owned
    )
    {
        List<HabbiconDefinitionSnapshot> missing = [];

        foreach (HabbiconDefinitionSnapshot entry in collection.Entries)
        {
            if (!owned.ContainsKey(entry.HabbiconId))
            {
                missing.Add(entry);
            }
        }

        return missing;
    }

    /// <summary>
    /// The state a grant should write, preserving a favourite. Re-granting something the player has
    /// already starred must not quietly un-star it.
    /// </summary>
    public static HabbiconState StateAfterGrant(HabbiconState current) =>
        current == HabbiconState.Favourite ? HabbiconState.Favourite : HabbiconState.Owned;

    /// <summary>Whether a state counts as the player owning the Habbicon and being able to use it.</summary>
    /// <remarks>
    /// <see cref="HabbiconState.Claimable"/> is deliberately excluded. The client shows an unclaimed
    /// bonus in the album, and a player who has not pressed claim does not have it — letting one be
    /// used would make the claim button decorative.
    /// </remarks>
    public static bool IsUsable(HabbiconState state) =>
        state is HabbiconState.Owned or HabbiconState.Favourite;
}
