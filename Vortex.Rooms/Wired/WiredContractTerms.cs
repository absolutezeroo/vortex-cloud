using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The custom-contract form, read into the terms a trading screen shows.
/// </summary>
/// <remarks>
/// Ten ints, five per side: enabled, coin(0)/furni(1), where the amount comes from, the amount, and
/// the variable's target. Which furni each side accepts comes in already resolved, because that
/// needs a room and this does not.
/// <para>
/// The two sides are not symmetric on the wire and so not here either: payment is a list of rules
/// and reward a single one, and rules are alternatives where the nodes inside one are a bundle. So
/// several furni to pay with are choices — any one of them settles the price — and several furni to
/// receive are all of them. The format has no way to say it the other way round.
/// </para>
/// </remarks>
public static class WiredContractTerms
{
    /// <summary>Five slots per side, payment first.</summary>
    private const int SideWidth = 5;

    private const int RewardOffset = 5;

    /// <summary>The form's own two element types.</summary>
    private const int CoinTerm = 0;

    /// <summary>The amount selector's "this is a plain number" option.</summary>
    private const int LiteralAmount = 0;

    /// <summary>
    /// Builds the terms, or refuses.
    /// </summary>
    /// <remarks>
    /// False means "do not offer", never "offer for free". It comes back for a form that was never
    /// filled in, for one with neither side enabled, and for an amount held in a wired variable —
    /// reading one needs a target and a context an offer does not have, and a term at the wrong
    /// amount is worse than no offer.
    /// </remarks>
    public static bool TryBuild(
        IReadOnlyList<int> intParams,
        IReadOnlyList<TradeContractItemType> paymentFurni,
        IReadOnlyList<TradeContractItemType> rewardFurni,
        int mode,
        int multiplier,
        out TradeContract? contract
    )
    {
        contract = null;

        if (intParams.Count < RewardOffset + SideWidth)
        {
            return false;
        }

        if (
            !TryBuildSide(intParams, 0, paymentFurni, out ImmutableArray<TradeContractRule> pay)
            || !TryBuildSide(
                intParams,
                RewardOffset,
                rewardFurni,
                out ImmutableArray<TradeContractRule> get
            )
        )
        {
            return false;
        }

        if (pay.IsEmpty && get.IsEmpty)
        {
            return false;
        }

        contract = new TradeContract
        {
            YouGiveRules = pay.IsEmpty ? null : pay,
            // The receive side is one rule: everything it names comes back together.
            YouGetRule = get.IsEmpty
                ? null
                : new TradeContractRule { Nodes = [.. get.SelectMany(rule => rule.Nodes)] },
            Mode = mode,
            Multiplier = multiplier,
            AutoMultiplierMax = multiplier,
        };

        return true;
    }

    /// <summary>
    /// One side's terms, or false when its amount is not a plain number.
    /// </summary>
    /// <remarks>
    /// A side that is switched off contributes nothing and is not a failure — a payment-only
    /// contract is exactly that, and so is a reward-only one. A furni side naming no furni is the
    /// same thing: there is no term to state.
    /// </remarks>
    private static bool TryBuildSide(
        IReadOnlyList<int> intParams,
        int offset,
        IReadOnlyList<TradeContractItemType> furni,
        out ImmutableArray<TradeContractRule> rules
    )
    {
        rules = ImmutableArray<TradeContractRule>.Empty;

        if (intParams[offset] == 0)
        {
            return true;
        }

        if (intParams[offset + 2] != LiteralAmount)
        {
            return false;
        }

        int amount = Math.Max(1, intParams[offset + 3]);

        if (intParams[offset + 1] == CoinTerm)
        {
            rules =
            [
                new TradeContractRule
                {
                    Nodes = [new TradeContractNode { IsFurni = false, Amount = amount }],
                },
            ];

            return true;
        }

        rules =
        [
            .. furni
                // The same kind twice is one choice, not two: the picker holds instances, and a
                // term names a kind.
                .DistinctBy(itemType => (itemType.IsWallItem, itemType.SpriteId))
                .Select(itemType => new TradeContractRule
                {
                    Nodes =
                    [
                        new TradeContractNode
                        {
                            IsFurni = true,
                            Amount = amount,
                            ItemType = itemType,
                        },
                    ],
                }),
        ];

        return true;
    }
}
