using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// This decides what a contract takes off a player and what it hands back, so every case here is a
/// case where getting it wrong means someone loses furniture or gets paid twice.
/// </summary>
public sealed class WiredContractSettlementTests
{
    private static readonly TradeContractItemType SofaKind = new()
    {
        IsWallItem = false,
        SpriteId = 100,
        LegacyPosterId = "",
    };

    private static readonly TradeContractItemType LampKind = new()
    {
        IsWallItem = false,
        SpriteId = 200,
        LegacyPosterId = "",
    };

    private static ContractItem Sofa(int id) => new(id, false, 100, "", false);

    private static ContractItem Lamp(int id) => new(id, false, 200, "", false);

    private static TradeContractNode Coins(int amount) =>
        new() { IsFurni = false, Amount = amount };

    private static TradeContractNode Furni(TradeContractItemType kind, int amount) =>
        new()
        {
            IsFurni = true,
            Amount = amount,
            ItemType = kind,
        };

    private static TradeContractRule Rule(params TradeContractNode[] nodes) =>
        new() { Nodes = [.. nodes] };

    [Fact]
    public void AStakeThatCoversTheOnlyRule_IsCharged()
    {
        TradeContract contract = new() { YouGiveRules = [Rule(Furni(SofaKind, 2))], Mode = 0 };

        ContractCharge? charge = WiredContractSettlement.MatchStake(
            contract,
            multiplier: 1,
            [Sofa(1), Sofa(2)]
        );

        charge!.ItemIds.Should().Equal(1, 2);
        charge.Coins.Should().Be(0);
    }

    /// <summary>One short is not "nearly", it is no.</summary>
    [Fact]
    public void AStakeOneShort_PaysNothing()
    {
        TradeContract contract = new() { YouGiveRules = [Rule(Furni(SofaKind, 2))], Mode = 0 };

        WiredContractSettlement.MatchStake(contract, multiplier: 1, [Sofa(1)]).Should().BeNull();
    }

    /// <summary>Putting up more than the price pays the price, not more.</summary>
    [Fact]
    public void ExtraStakedFurniture_IsLeftAlone()
    {
        TradeContract contract = new() { YouGiveRules = [Rule(Furni(SofaKind, 1))], Mode = 0 };

        ContractCharge? charge = WiredContractSettlement.MatchStake(
            contract,
            multiplier: 1,
            [Sofa(1), Sofa(2), Lamp(3)]
        );

        charge!.ItemIds.Should().Equal(1);
    }

    /// <summary>Alternatives are alternatives: the first one covered is the one charged.</summary>
    [Fact]
    public void TheFirstCoveredAlternative_IsTheOneCharged()
    {
        TradeContract contract = new()
        {
            YouGiveRules = [Rule(Furni(SofaKind, 3)), Rule(Furni(LampKind, 1))],
            Mode = 0,
        };

        ContractCharge? charge = WiredContractSettlement.MatchStake(
            contract,
            multiplier: 1,
            [Sofa(1), Lamp(2)]
        );

        // The sofa rule was not covered; the lamp one was.
        charge!.ItemIds.Should().Equal(2);
    }

    /// <summary>The nodes inside one rule are a bundle: half of it buys nothing.</summary>
    [Fact]
    public void ARuleHalfCovered_IsNotCovered()
    {
        TradeContract contract = new()
        {
            YouGiveRules = [Rule(Furni(SofaKind, 1), Furni(LampKind, 1))],
            Mode = 0,
        };

        WiredContractSettlement.MatchStake(contract, multiplier: 1, [Sofa(1)]).Should().BeNull();
    }

    /// <summary>Taking a contract three times costs three times, on both kinds of term.</summary>
    [Fact]
    public void TheMultiplier_ScalesFurniAndCoinsAlike()
    {
        TradeContract contract = new()
        {
            YouGiveRules = [Rule(Furni(SofaKind, 2), Coins(10))],
            Mode = 1,
            Multiplier = 3,
        };

        ContractCharge? charge = WiredContractSettlement.MatchStake(
            contract,
            multiplier: 3,
            [Sofa(1), Sofa(2), Sofa(3), Sofa(4), Sofa(5), Sofa(6)]
        );

        charge!.ItemIds.Should().HaveCount(6);
        charge.Coins.Should().Be(30);
    }

    [Fact]
    public void AMultipliedStakeOneShort_PaysNothing()
    {
        TradeContract contract = new() { YouGiveRules = [Rule(Furni(SofaKind, 2))], Mode = 1 };

        WiredContractSettlement
            .MatchStake(contract, multiplier: 3, [Sofa(1), Sofa(2), Sofa(3), Sofa(4), Sofa(5)])
            .Should()
            .BeNull();
    }

    /// <summary>A contract that asks for nothing is satisfied by an empty table, not refused.</summary>
    [Fact]
    public void AContractThatAsksForNothing_IsSatisfiedByAnEmptyTable()
    {
        TradeContract contract = new() { YouGetRule = Rule(Coins(5)), Mode = 0 };

        WiredContractSettlement
            .MatchStake(contract, multiplier: 1, [])
            .Should()
            .Be(ContractCharge.Nothing);
    }

    /// <summary>
    /// A furni term with no kind on it names nothing. Taking nothing for it would hand the contract
    /// over free, so the whole rule is refused instead.
    /// </summary>
    [Fact]
    public void AFurniTermWithNoKind_RefusesItsRule()
    {
        TradeContract contract = new()
        {
            YouGiveRules = [Rule(new TradeContractNode { IsFurni = true, Amount = 1 })],
            Mode = 0,
        };

        WiredContractSettlement.MatchStake(contract, multiplier: 1, [Sofa(1)]).Should().BeNull();
    }

    [Fact]
    public void AChestWithTheStock_PaysTheReward()
    {
        TradeContract contract = new()
        {
            YouGetRule = Rule(Furni(LampKind, 1), Coins(20)),
            Mode = 0,
        };

        bool reserved = WiredContractSettlement.TryReserveReward(
            contract,
            multiplier: 2,
            [Lamp(7), Lamp(8), Sofa(9)],
            chestCredits: 100,
            out ContractCharge reward
        );

        reserved.Should().BeTrue();
        reward.ItemIds.Should().Equal(7, 8);
        reward.Coins.Should().Be(40);
    }

    /// <summary>A shop that has run out is not a shop that gives credit.</summary>
    [Fact]
    public void AChestShortOfStock_PaysNothingAtAll()
    {
        TradeContract contract = new() { YouGetRule = Rule(Furni(LampKind, 2)), Mode = 0 };

        WiredContractSettlement
            .TryReserveReward(
                contract,
                multiplier: 1,
                [Lamp(7)],
                chestCredits: 0,
                out ContractCharge reward
            )
            .Should()
            .BeFalse();

        reward.Should().Be(ContractCharge.Nothing);
    }

    [Fact]
    public void AChestShortOfCoins_PaysNothingAtAll()
    {
        TradeContract contract = new() { YouGetRule = Rule(Coins(50)), Mode = 0 };

        WiredContractSettlement
            .TryReserveReward(contract, multiplier: 1, [], chestCredits: 49, out ContractCharge _)
            .Should()
            .BeFalse();
    }

    /// <summary>A payment-only contract owes nothing, and an empty chest can pay that.</summary>
    [Fact]
    public void APaymentOnlyContract_OwesNothing()
    {
        TradeContract contract = new() { YouGiveRules = [Rule(Coins(5))], Mode = 0 };

        WiredContractSettlement
            .TryReserveReward(
                contract,
                multiplier: 1,
                [],
                chestCredits: 0,
                out ContractCharge reward
            )
            .Should()
            .BeTrue();

        reward.IsNothing.Should().BeTrue();
    }

    /// <summary>
    /// Two posters of the same sprite are two different things, and the chest's own withdrawal
    /// already tells them apart that way.
    /// </summary>
    [Fact]
    public void PostersAreToldApartByTheirNumber()
    {
        TradeContractItemType poster = new()
        {
            IsWallItem = true,
            SpriteId = 300,
            LegacyPosterId = "42",
        };

        TradeContract contract = new() { YouGiveRules = [Rule(Furni(poster, 1))], Mode = 0 };

        List<ContractItem> wrongPoster = [new(1, true, 300, "7", true)];
        List<ContractItem> rightPoster = [new(2, true, 300, "42", true)];

        WiredContractSettlement.MatchStake(contract, 1, wrongPoster).Should().BeNull();
        WiredContractSettlement.MatchStake(contract, 1, rightPoster)!.ItemIds.Should().Equal(2);
    }

    /// <summary>Two terms for the same kind add up rather than reusing the same item twice.</summary>
    [Fact]
    public void TwoTermsForOneKind_DoNotShareItems()
    {
        TradeContract contract = new()
        {
            YouGiveRules = [Rule(Furni(SofaKind, 1), Furni(SofaKind, 1))],
            Mode = 0,
        };

        WiredContractSettlement.MatchStake(contract, 1, [Sofa(1)]).Should().BeNull();

        WiredContractSettlement
            .MatchStake(contract, 1, [Sofa(1), Sofa(2)])!
            .ItemIds.Should()
            .Equal(1, 2);
    }
}
