using System;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Primitives.Players.Wallet;

/// <summary>
/// The reward encoding every piece of admin-authored content shares when it pays a currency —
/// achievement levels, quests, daily tasks: a <b>negative</b> reward type means credits, any other
/// value is the activity-point type of the currency to pay in.
///
/// <para>
/// It used to live as an inline <c>type &lt; 0</c> branch at each grant site, which left nothing to
/// answer the question the admin surfaces actually need: <i>will this reward be paid at all?</i> A
/// grant resolves its currency through <see cref="ICurrencyTypeProvider"/> and <b>does nothing</b>
/// when that row is missing or disabled, so a reward configured against a currency this hotel does
/// not have is a promise the player silently never gets. <see cref="Validate"/> is that question,
/// asked once at the moment the content is saved.
/// </para>
/// </summary>
public static class CurrencyRewardRules
{
    /// <summary>Credits, as the reward encoding writes them. Any negative value reads the same.</summary>
    public const int CreditsRewardType = -1;

    /// <summary>How the string form (daily-task rewards) spells credits.</summary>
    public const string CreditsRewardTypeId = "credits";

    /// <summary>The wallet currency a reward type names.</summary>
    public static CurrencyKind KindFor(int rewardType) =>
        rewardType < 0
            ? new CurrencyKind { CurrencyType = CurrencyType.Credits }
            : new CurrencyKind
            {
                CurrencyType = CurrencyType.ActivityPoints,
                ActivityPointType = rewardType,
            };

    /// <summary>
    /// Null when a reward of <paramref name="amount"/> in <paramref name="rewardType"/> would really
    /// be paid; otherwise the error code the admin surface reports. An amount of zero configures no
    /// reward at all, so there is nothing to check.
    /// </summary>
    public static string? Validate(ICurrencyTypeProvider currencyTypes, int rewardType, int amount)
    {
        if (amount <= 0)
        {
            return null;
        }

        CurrencyTypeSnapshot? currency = currencyTypes.GetCurrencyTypeByKind(KindFor(rewardType));

        if (currency is null)
        {
            return "reward_currency_unknown";
        }

        return currency.Enabled ? null : "reward_currency_disabled";
    }

    /// <summary>
    /// The same check for the string form daily tasks store. A reward type that names neither
    /// credits nor an activity-point number is not a currency at all (an item code, which the task
    /// grain reports separately), so this rule has nothing to say about it.
    /// </summary>
    public static string? ValidateNamed(
        ICurrencyTypeProvider currencyTypes,
        string? rewardTypeId,
        int amount
    ) =>
        TryParseNamed(rewardTypeId, out int rewardType)
            ? Validate(currencyTypes, rewardType, amount)
            : null;

    /// <summary>
    /// Reads the string form into the numeric one: <c>"credits"</c> or an activity-point type
    /// number. False for anything else, which is not a currency reward.
    /// </summary>
    public static bool TryParseNamed(string? rewardTypeId, out int rewardType)
    {
        if (
            string.Equals(
                rewardTypeId?.Trim(),
                CreditsRewardTypeId,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            rewardType = CreditsRewardType;
            return true;
        }

        return int.TryParse(rewardTypeId, out rewardType) && rewardType >= 0;
    }
}
