using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.Bots;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// Habbo's "bot reaches furni" trigger: fires when the named bot steps onto a tile holding one of
/// the stack's selected items. The room publishes every bot arrival and the matching is done here,
/// because the room does not know which furni a given stack has selected.
/// </summary>
[RoomObjectLogic("wf_trg_bot_reached_stf")]
public class WiredTriggerBotReachesItem(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.BOT_DESTINATION_REACHED;

    public override List<Type> SupportedEventTypes { get; } = [typeof(BotReachedTileEvent)];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [WiredPlayerSourceType.BotByName, WiredPlayerSourceType.SelectorUsers],
        ];

    public override async Task<bool> CanTriggerAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.Event is not BotReachedTileEvent evt || !MatchesConfiguredBot(evt.BotName))
        {
            return false;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        return selection.SelectedFurniIds.Any(furniId => IsOnTile(furniId, evt.TileIdx));
    }

    /// <summary>
    /// The name typed into the form. A blank form means any bot, which is what an unconfigured
    /// trigger has always meant elsewhere.
    /// </summary>
    private bool MatchesConfiguredBot(string botName)
    {
        string configured = (_wiredData.StringParam ?? string.Empty).Trim();

        return configured.Length == 0
            || configured.Equals(botName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsOnTile(int furniId, int tileIdx) =>
        _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
        && item is IRoomFloorItem floor
        && _ctx.Map.ToIdx(floor.X, floor.Y) == tileIdx;
}
