using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>One piece of furniture, as a contract's terms see it: a kind and an identity.</summary>
/// <remarks>
/// A term names a kind and the trade moves an item, so both are needed. The poster number only
/// distinguishes posters, which is the same test the chest's own withdrawal uses — every other kind
/// of furniture carries one that means nothing.
/// </remarks>
public readonly record struct ContractItem(
    int Id,
    bool IsWallItem,
    int SpriteId,
    string LegacyPosterId,
    bool IsPoster
)
{
    public bool Matches(TradeContractItemType kind) =>
        SpriteId == kind.SpriteId
        && IsWallItem == kind.IsWallItem
        && (!IsPoster || LegacyPosterId == kind.LegacyPosterId);
}

/// <summary>Coins and furniture moving one way.</summary>
public sealed record ContractCharge(int Coins, ImmutableArray<int> ItemIds)
{
    public static readonly ContractCharge Nothing = new(0, []);

    public bool IsNothing => Coins == 0 && ItemIds.IsEmpty;
}

/// <summary>
/// What a contract takes and what it gives, worked out before anything moves.
/// </summary>
/// <remarks>
/// Both sides are decided here and answered as exact item ids rather than as counts, so that "the
/// stake is enough" and "these are the ones that move" cannot disagree — a settlement that recounts
/// later is a settlement that can take the wrong sofa.
/// </remarks>
public static class WiredContractSettlement
{
    /// <summary>
    /// The first alternative the stake satisfies, or null when it satisfies none.
    /// </summary>
    /// <remarks>
    /// The give side is a list of alternatives, so a contract naming three ways to pay is paid one
    /// of the three — never all three. A contract with no give side asks for nothing, which an
    /// empty table satisfies.
    /// <para>
    /// Extra staked furniture is left where it is: a player who puts up more than the price pays
    /// the price.
    /// </para>
    /// </remarks>
    public static ContractCharge? MatchStake(
        TradeContract contract,
        int multiplier,
        IReadOnlyList<ContractItem> staked
    )
    {
        if (contract.YouGiveRules is not { } rules || rules.IsEmpty)
        {
            return ContractCharge.Nothing;
        }

        foreach (TradeContractRule rule in rules)
        {
            if (TryTake(rule, multiplier, staked, out ContractCharge? charge))
            {
                return charge;
            }
        }

        return null;
    }

    /// <summary>
    /// What the contract owes, and whether the chest behind it can actually pay.
    /// </summary>
    /// <remarks>
    /// False means the trade does not complete and nothing moves — a shop that has run out is not a
    /// shop that gives credit. A contract with no receive side owes nothing and always can.
    /// </remarks>
    public static bool TryReserveReward(
        TradeContract contract,
        int multiplier,
        IReadOnlyList<ContractItem> stock,
        int chestCredits,
        out ContractCharge reward
    )
    {
        reward = ContractCharge.Nothing;

        if (contract.YouGetRule is not { } rule)
        {
            return true;
        }

        if (!TryTake(rule, multiplier, stock, out ContractCharge? owed))
        {
            return false;
        }

        if (owed!.Coins > chestCredits)
        {
            return false;
        }

        reward = owed;

        return true;
    }

    /// <summary>
    /// One rule against one pile: the coins it comes to and the exact items it would take.
    /// </summary>
    /// <remarks>
    /// The nodes inside a rule are a bundle, so every one of them has to be met — a rule half
    /// covered is not covered. Amounts are per taking of the contract, hence the multiplier.
    /// </remarks>
    private static bool TryTake(
        TradeContractRule rule,
        int multiplier,
        IReadOnlyList<ContractItem> pile,
        out ContractCharge? charge
    )
    {
        charge = null;

        int coins = 0;
        List<int> taking = [];

        foreach (TradeContractNode node in rule.Nodes)
        {
            int wanted = node.Amount * multiplier;

            if (!node.IsFurni)
            {
                coins += wanted;

                continue;
            }

            // A furni term with no kind on it names nothing, and taking nothing for it would hand
            // the contract over for free.
            if (node.ItemType is not { } kind)
            {
                return false;
            }

            List<int> matching =
            [
                .. pile.Where(item => !taking.Contains(item.Id) && item.Matches(kind))
                    .Select(item => item.Id)
                    .Take(wanted),
            ];

            if (matching.Count < wanted)
            {
                return false;
            }

            taking.AddRange(matching);
        }

        charge = new ContractCharge(coins, [.. taking]);

        return true;
    }
}
