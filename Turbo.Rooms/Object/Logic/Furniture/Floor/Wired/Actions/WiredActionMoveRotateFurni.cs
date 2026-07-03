using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

[RoomObjectLogic("wf_act_move_rotate")]
public class WiredActionMoveRotateFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_AND_ROTATE_FURNI;

    private int _movementType = 0;
    private int _rotationType = 0;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 7, 0), // Movement Type
            new WiredRangeParamRule(0, 3, 0), // Rotation Type
        ];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        ActionContext actionCtx = ctx.AsActionContext();

        foreach (int furniId in selection.SelectedFurniIds)
        {
            try
            {
                if (
                    !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                    || item is not IRoomFloorItem floorItem
                )
                {
                    continue;
                }

                Rotation moveDirection = GetMoveDirection(_movementType);
                Rotation moveRotation = GetMoveRotation(floorItem.Rotation, _rotationType);

                if (
                    !_roomGrain.MapModule.TryGetTileInFront(
                        _roomGrain.MapModule.ToIdx(floorItem.X, floorItem.Y),
                        moveDirection,
                        out int nextIdx
                    )
                )
                {
                    continue;
                }

                (int targetX, int targetY) = _roomGrain.MapModule.GetTileXY(nextIdx);

                if (
                    !await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                        actionCtx,
                        furniId,
                        targetX,
                        targetY,
                        moveRotation
                    )
                )
                {
                    continue;
                }

                await ctx.ProcessFloorItemMovementAsync(floorItem, nextIdx, null, moveRotation);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to move/rotate furni {FurniId} via wired action on item {ItemId}.",
                    furniId,
                    _ctx.ObjectId
                );
                continue;
            }
        }

        return true;
    }

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _movementType = _wiredData.GetIntParam<int>(0);
            _rotationType = _wiredData.GetIntParam<int>(1);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Malformed move/rotate params for wired item {ItemId}; keeping current defaults.",
                _ctx.ObjectId
            );
        }
    }

    public static Rotation GetMoveDirection(int movementType) =>
        movementType switch
        {
            1 => RotationExtensions.CARDINAL[Random.Shared.Next(0, 4)],
            2 => Random.Shared.NextDouble() < 0.5 ? Rotation.East : Rotation.West,
            3 => Random.Shared.NextDouble() < 0.5 ? Rotation.North : Rotation.South,
            4 => Rotation.North,
            5 => Rotation.East,
            6 => Rotation.South,
            7 => Rotation.West,
            _ => Rotation.None,
        };

    public static Rotation GetMoveRotation(Rotation currentRotation, int rotationType) =>
        rotationType switch
        {
            1 => currentRotation.Rotate(+2),
            2 => currentRotation.Rotate(-2),
            3 => GetMoveRotation(currentRotation, Random.Shared.Next(2) == 0 ? 1 : 2),
            _ => currentRotation,
        };
}
