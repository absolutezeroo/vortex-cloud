using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// A wired trading contract: what a player pays, and what they get back.
/// </summary>
/// <remarks>
/// The three contract furni share one form — payment and reward are each an enable checkbox, a
/// coin/furni radio and an amount — so <c>wf_contract_payment</c> is that form with the reward half
/// left off, <c>wf_contract_reward</c> with the payment half left off, and
/// <c>wf_contract_trade</c> with both. Nothing here executes: the box holds the terms, and
/// <c>wf_act_init_transaction</c> is what offers them.
/// <para>
/// Ten ints, five per side: enabled, coin(0)/furni(1), where the amount comes from, the amount, and
/// the variable's target. The furni a side accepts are its own picker's — the payment side's in
/// <c>StuffIds</c>, the reward side's in <c>StuffIds2</c> — which is why both containers are
/// declared: without that the client renders no picker and a furni term can never name a furni.
/// </para>
/// <para>
/// These three shipped bound to <c>furniture_basic</c>, a name shared with roughly 5 800 other
/// definitions and therefore useless to match on;
/// <c>scripts/sql/wired_contract_logic_binding.sql</c> points them at these names instead. The
/// boxes that offer and cancel transactions find the contract among the furni they were pointed at
/// by that logic name — a classname is not a key in this database.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_custom_contract")]
public class WiredAddonCustomContract(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The amount section's own bounds, from the client's form.</summary>
    private const int MaxAmount = 100000;

    public override int WiredCode => (int)WiredAddonType.CUSTOM_CONTRACT;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // payment enabled
            new WiredRangeParamRule(0, 1, 0), // payment element type
            new WiredRangeParamRule(0, int.MaxValue, 0), // payment amount source
            new WiredRangeParamRule(1, MaxAmount, 1), // payment amount
            new WiredRangeParamRule(0, int.MaxValue, 0), // payment amount source target
            new WiredRangeParamRule(0, 1, 0), // reward enabled
            new WiredRangeParamRule(0, 1, 0), // reward element type
            new WiredRangeParamRule(0, int.MaxValue, 0), // reward amount source
            new WiredRangeParamRule(1, MaxAmount, 1), // reward amount
            new WiredRangeParamRule(0, int.MaxValue, 0), // reward amount source target
        ];

    /// <summary>One per side, and they are read as the furni each side accepts.</summary>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems],
            [WiredFurniSourceType.SelectedItems],
        ];

    /// <summary>An amount apiece, when it is read off a variable rather than typed.</summary>
    public override int GetMaxVariableIds() => 2;
}
