using System.Collections.Generic;
using System.Linq;
using Orleans;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "WIRED Add-on: Custom Contract" — what a player pays, and what they get back.
/// </summary>
/// <remarks>
/// Payment and reward are each an enable checkbox, a coin/furni radio and an amount, so one form
/// covers a contract that only takes, one that only gives, and one that does both. Nothing here
/// executes: the box holds the terms, and <c>wf_act_init_transaction</c> in the same stack is what
/// offers them.
/// <para>
/// Ten ints, five per side: enabled, coin(0)/furni(1), where the amount comes from, the amount, and
/// the variable's target. The furni a side accepts are its own picker's — the payment side's in
/// <c>StuffIds</c>, the reward side's in <c>StuffIds2</c> — which is why both containers are
/// declared: without that the client renders no picker and a furni term can never name a furni.
/// </para>
/// <para>
/// The <c>wf_contract_*</c> furni are not this. They are what the offering box points at to say
/// which contract a trade is about, they ship as plain furniture, and they carry no form — which is
/// why the terms are read here and not off them.
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

    /// <summary>
    /// Turns this form into the terms the trading screen reads.
    /// </summary>
    /// <remarks>
    /// The reading itself is <see cref="WiredContractTerms" />; what belongs here is the half that
    /// needs a room — each side's picker holds furni <em>instances</em> and a term names a
    /// <em>kind</em>, so the ids are resolved to definitions before the form is read. False means
    /// "do not offer", never "offer for free".
    /// </remarks>
    public bool TryBuildContract(int mode, int multiplier, out TradeContract? contract) =>
        WiredContractTerms.TryBuild(
            _wiredData.IntParams,
            ResolveItemTypes(GetStuffIds()),
            ResolveItemTypes(GetStuffIds2()),
            mode,
            multiplier,
            out contract
        );

    /// <summary>What kinds of furniture a picker's chosen instances are. Gone ones drop out.</summary>
    private List<TradeContractItemType> ResolveItemTypes(List<int> furniIds) =>
        [
            .. furniIds
                .Select(furniId =>
                    _ctx.Lookup.TryFindItem(furniId, out IRoomItem? furni) ? furni : null
                )
                .Where(furni => furni is not null)
                .Select(furni => new TradeContractItemType
                {
                    IsWallItem = furni!.Definition.ProductType == ProductType.Wall,
                    SpriteId = furni.Definition.SpriteId,
                    LegacyPosterId = string.Empty,
                }),
        ];
}
