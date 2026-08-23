using System.Collections.Generic;
using System.Linq;
using Orleans;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;
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

    /// <summary>
    /// Four slots, in the order the client's form addresses them.
    /// </summary>
    /// <remarks>
    /// Slots 0 and 1 are the furni each side accepts -- payment, then reward -- and stay on the
    /// pick list alone, because they name furni the player chose rather than a source to resolve.
    /// Slots 2 and 3 are each side's amount when it is read off a variable instead of typed: the
    /// "a plain number / whatever this variable says" selectors are merged input sources, and the
    /// client addresses them as furni 2 / player 0 and furni 3 / player 1
    /// (<c>mergedSelections()</c> returns <c>[[2, 0], [3, 1]]</c> for this form).
    /// <para>
    /// Stopping at two left both amount arrows reading past the end of the list they were handed,
    /// which neither this client nor the Flash one it is ported from range-checks.
    /// </para>
    /// </remarks>
    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems],
            [WiredFurniSourceType.SelectedItems],
            [.. AmountFurniSources],
            [.. AmountFurniSources],
        ];

    /// <inheritdoc cref="GetAllowedFurniSources"/>
    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [.. AmountPlayerSources],
            [.. AmountPlayerSources],
        ];

    /// <summary>Where an amount read off a variable may be read from.</summary>
    private static readonly WiredFurniSourceType[] AmountFurniSources =
    [
        WiredFurniSourceType.SelectedItems,
        WiredFurniSourceType.SelectorItems,
        WiredFurniSourceType.SignalItems,
        WiredFurniSourceType.TriggeredItem,
    ];

    /// <inheritdoc cref="AmountFurniSources"/>
    private static readonly WiredPlayerSourceType[] AmountPlayerSources =
    [
        WiredPlayerSourceType.TriggeredUser,
        WiredPlayerSourceType.SelectorUsers,
        WiredPlayerSourceType.SignalUsers,
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
