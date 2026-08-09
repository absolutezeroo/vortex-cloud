using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

public abstract class FurnitureWiredActionLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredLogic(grainFactory, stuffDataFactory, ctx), IWiredAction
{
    public override WiredType WiredType => WiredType.Action;

    private int _delayMs = 0;

    public override List<Type> GetDefinitionSpecificTypes() =>
        [.. base.GetDefinitionSpecificTypes(), typeof(int)];

    public int GetDelayMs() => _delayMs;

    public virtual Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct) =>
        Task.FromResult(true);

    /// <summary>
    /// The tile of the first selected floor item, which is how every "send something to the furni"
    /// action reads its destination. False when the selection holds no floor item — a stack whose
    /// target furni has been picked up, which is ordinary rather than an error.
    /// </summary>
    protected bool TryResolveDestinationTile(IWiredSelectionSet selection, out int tileIdx)
    {
        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                _ctx.Lookup.TryFindItem(furniId, out IRoomItem? item)
                && item is IRoomFloorItem floor
            )
            {
                tileIdx = _ctx.Map.ToIdx(floor.X, floor.Y);

                return true;
            }
        }

        tileIdx = 0;

        return false;
    }

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _delayMs = Math.Clamp(_wiredData.GetDefinitionParam<int>(0), 0, 20) * 500;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Malformed action delay param for wired item {ItemId}; keeping current default.",
                _ctx.ObjectId
            );
        }
    }
}
