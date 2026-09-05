using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Vortex.Habbicons;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// Collection completion and its bonus. Everything about whether a set is finished is derived from
/// ownership here, and never stored — these are the tests that keep it that way honest.
/// </summary>
public class HabbiconCollectionRulesTests
{
    private static readonly HabbiconCollectionSnapshot ThreeAndABonus = Content.Collection(
        collectionId: 1,
        entryCount: 3
    );

    [Fact]
    public void A_set_is_incomplete_until_every_entry_is_owned()
    {
        HabbiconCollectionRules
            .IsComplete(ThreeAndABonus, Content.Owned(101, 102))
            .Should()
            .BeFalse();

        HabbiconCollectionRules
            .IsComplete(ThreeAndABonus, Content.Owned(101, 102, 103))
            .Should()
            .BeTrue();
    }

    /// <summary>
    /// A set with no entries is unfinished content, not a finished set. Saying otherwise would hand
    /// its bonus to everyone the moment it was created.
    /// </summary>
    [Fact]
    public void An_empty_set_is_never_complete()
    {
        HabbiconCollectionSnapshot empty = ThreeAndABonus with { Entries = [] };

        HabbiconCollectionRules.IsComplete(empty, Content.Owned()).Should().BeFalse();
    }

    /// <summary>The bonus does not count towards its own set — that would make the set uncompletable.</summary>
    [Fact]
    public void The_bonus_is_not_one_of_the_entries()
    {
        ThreeAndABonus.Entries.Should().HaveCount(3);
        ThreeAndABonus.RewardHabbicon!.HabbiconId.Should().Be(199);
        ThreeAndABonus.Entries.Should().NotContain(e => e.HabbiconId == 199);
    }

    [Fact]
    public void The_bonus_becomes_claimable_when_the_set_fills_up()
    {
        HabbiconCollectionRules
            .ResolveRewardState(ThreeAndABonus, Content.Owned(101, 102))
            .Should()
            .Be(HabbiconState.NotOwned);

        HabbiconCollectionRules
            .ResolveRewardState(ThreeAndABonus, Content.Owned(101, 102, 103))
            .Should()
            .Be(HabbiconState.Claimable);
    }

    [Fact]
    public void A_claimed_bonus_reports_owned_rather_than_claimable()
    {
        Dictionary<int, HabbiconState> owned = Content.Owned(101, 102, 103, 199);

        HabbiconCollectionRules
            .ResolveRewardState(ThreeAndABonus, owned)
            .Should()
            .Be(HabbiconState.Owned);

        HabbiconCollectionRules.CanClaimReward(ThreeAndABonus, owned).Should().BeFalse();
    }

    /// <summary>
    /// A player who claimed the bonus and then lost an entry — an operator revoke, a content edit —
    /// keeps what they were given. Taking a reward back because the content changed underneath
    /// somebody is not correctness anybody wants.
    /// </summary>
    [Fact]
    public void Losing_an_entry_afterwards_does_not_take_the_bonus_back()
    {
        Dictionary<int, HabbiconState> owned = Content.Owned(101, 102, 199);

        HabbiconCollectionRules.IsComplete(ThreeAndABonus, owned).Should().BeFalse();
        HabbiconCollectionRules
            .ResolveRewardState(ThreeAndABonus, owned)
            .Should()
            .Be(HabbiconState.Owned);
    }

    [Fact]
    public void A_set_with_no_bonus_has_nothing_to_claim()
    {
        HabbiconCollectionSnapshot noBonus = Content.Collection(2, 2, withReward: false);

        HabbiconCollectionRules.IsComplete(noBonus, Content.Owned(201, 202)).Should().BeTrue();
        HabbiconCollectionRules
            .ResolveRewardState(noBonus, Content.Owned(201, 202))
            .Should()
            .Be(HabbiconState.NotOwned);
        HabbiconCollectionRules.CanClaimReward(noBonus, Content.Owned(201, 202)).Should().BeFalse();
    }

    [Fact]
    public void Missing_entries_are_what_a_whole_set_purchase_buys()
    {
        List<HabbiconDefinitionSnapshot> missing = HabbiconCollectionRules.MissingEntries(
            ThreeAndABonus,
            Content.Owned(102)
        );

        missing.Select(m => m.HabbiconId).Should().Equal(101, 103);
        missing.Should().NotContain(m => m.HabbiconId == 199, "the bonus is claimed, never bought");
    }

    [Fact]
    public void Re_granting_a_favourite_does_not_un_star_it()
    {
        HabbiconCollectionRules
            .StateAfterGrant(HabbiconState.Favourite)
            .Should()
            .Be(HabbiconState.Favourite);

        HabbiconCollectionRules
            .StateAfterGrant(HabbiconState.Owned)
            .Should()
            .Be(HabbiconState.Owned);
    }

    /// <summary>
    /// An unclaimed bonus cannot be used. The client shows it in the album with a claim button, and
    /// letting it be used would make that button decorative.
    /// </summary>
    [Fact]
    public void A_claimable_habbicon_is_not_usable()
    {
        HabbiconCollectionRules.IsUsable(HabbiconState.Claimable).Should().BeFalse();
        HabbiconCollectionRules.IsUsable(HabbiconState.NotOwned).Should().BeFalse();
        HabbiconCollectionRules.IsUsable(HabbiconState.Owned).Should().BeTrue();
        HabbiconCollectionRules.IsUsable(HabbiconState.Favourite).Should().BeTrue();
    }
}
