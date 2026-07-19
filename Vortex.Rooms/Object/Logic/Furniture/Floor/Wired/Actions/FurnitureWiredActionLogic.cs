using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
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

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _delayMs = Math.Clamp(_wiredData.GetDefinitionParam<int>(0), 0, 20) * 500;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Malformed action delay param for wired item {ItemId}; keeping current default.",
                _ctx.ObjectId
            );
        }
    }
}
