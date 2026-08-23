using System.Collections.Generic;
using FluentAssertions;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The custom-contract form decides what a player is asked to pay, so every way it can be filled in
/// has to come out saying what the author meant — and every way it cannot be honoured has to come
/// out as no offer at all rather than as a cheaper one.
/// </summary>
public sealed class WiredContractTermsTests
{
    /// <summary>enabled, coin(0)/furni(1), amount source, amount, source target — twice.</summary>
    private static List<int> Form(
        int payEnabled = 0,
        int payType = 0,
        int paySource = 0,
        int payAmount = 1,
        int getEnabled = 0,
        int getType = 0,
        int getSource = 0,
        int getAmount = 1
    ) =>
        [
            payEnabled,
            payType,
            paySource,
            payAmount,
            0,
            getEnabled,
            getType,
            getSource,
            getAmount,
            0,
        ];

    private static readonly TradeContractItemType Sofa = new()
    {
        IsWallItem = false,
        SpriteId = 1234,
        LegacyPosterId = "",
    };

    private static readonly TradeContractItemType Poster = new()
    {
        IsWallItem = true,
        SpriteId = 4321,
        LegacyPosterId = "",
    };

    [Fact]
    public void CoinsForFurni_ReadsBothSides()
    {
        bool built = WiredContractTerms.TryBuild(
            Form(payEnabled: 1, payType: 0, payAmount: 50, getEnabled: 1, getType: 1, getAmount: 2),
            [],
            [Sofa],
            mode: 1,
            multiplier: 3,
            out TradeContract? contract
        );

        built.Should().BeTrue();
        contract!.YouGiveRules!.Value.Should().HaveCount(1);
        contract.YouGiveRules.Value[0].Nodes[0].IsFurni.Should().BeFalse();
        contract.YouGiveRules.Value[0].Nodes[0].Amount.Should().Be(50);
        contract.YouGetRule!.Nodes.Should().HaveCount(1);
        contract.YouGetRule.Nodes[0].ItemType.Should().Be(Sofa);
        contract.YouGetRule.Nodes[0].Amount.Should().Be(2);
        contract.Multiplier.Should().Be(3);
    }

    /// <summary>Two furni to pay with are two ways to pay, not a bundle of both.</summary>
    [Fact]
    public void SeveralFurniToPayWith_BecomeAlternatives()
    {
        WiredContractTerms.TryBuild(
            Form(payEnabled: 1, payType: 1, payAmount: 1),
            [Sofa, Poster],
            [],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        contract!.YouGiveRules!.Value.Should().HaveCount(2, "one rule apiece is one choice apiece");
        contract.YouGiveRules.Value[0].Nodes.Should().HaveCount(1);
        contract.YouGiveRules.Value[1].Nodes.Should().HaveCount(1);
    }

    /// <summary>Two furni to receive all come back, which the wire can only say as one rule.</summary>
    [Fact]
    public void SeveralFurniToReceive_BecomeOneBundle()
    {
        WiredContractTerms.TryBuild(
            Form(getEnabled: 1, getType: 1, getAmount: 1),
            [],
            [Sofa, Poster],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        contract!.YouGetRule!.Nodes.Should().HaveCount(2);
        contract.YouGiveRules.Should().BeNull("a reward-only contract asks for nothing");
    }

    /// <summary>The picker holds instances; the same kind twice is still one way to pay.</summary>
    [Fact]
    public void TheSameKindTwice_IsOneChoice()
    {
        WiredContractTerms.TryBuild(
            Form(payEnabled: 1, payType: 1),
            [Sofa, Sofa],
            [],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        contract!.YouGiveRules!.Value.Should().HaveCount(1);
    }

    [Fact]
    public void APaymentOnlyContract_HasNoReceiveSide()
    {
        WiredContractTerms.TryBuild(
            Form(payEnabled: 1, payAmount: 10),
            [],
            [],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        contract!.YouGetRule.Should().BeNull();
    }

    /// <summary>
    /// An amount read off a wired variable cannot be honoured here, and the refusal is the point:
    /// dropping the term would ask for nothing and hand over the reward anyway.
    /// </summary>
    [Fact]
    public void AVariableAmount_RefusesTheWholeContract()
    {
        bool built = WiredContractTerms.TryBuild(
            Form(payEnabled: 1, paySource: 1, payAmount: 50, getEnabled: 1, getType: 1),
            [],
            [Sofa],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        built.Should().BeFalse();
        contract.Should().BeNull();
    }

    [Fact]
    public void AFormWithNeitherSideOn_IsNotAnOffer()
    {
        WiredContractTerms
            .TryBuild(Form(), [], [], mode: 0, multiplier: 1, out TradeContract? _)
            .Should()
            .BeFalse();
    }

    /// <summary>A box that was placed and never opened has no params at all.</summary>
    [Fact]
    public void AFormThatWasNeverFilledIn_IsNotAnOffer()
    {
        WiredContractTerms
            .TryBuild([], [], [], mode: 0, multiplier: 1, out TradeContract? _)
            .Should()
            .BeFalse();
    }

    /// <summary>A furni side that names no furni states no term, and does not fail either.</summary>
    [Fact]
    public void AFurniSideWithNothingPicked_ContributesNothing()
    {
        bool built = WiredContractTerms.TryBuild(
            Form(payEnabled: 1, payType: 1, getEnabled: 1, getType: 0, getAmount: 5),
            [],
            [],
            mode: 0,
            multiplier: 1,
            out TradeContract? contract
        );

        built.Should().BeTrue("the reward side still says something");
        contract!.YouGiveRules.Should().BeNull();
        contract.YouGetRule!.Nodes[0].Amount.Should().Be(5);
    }
}
