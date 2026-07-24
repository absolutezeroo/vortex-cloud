using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Moves the selected furni onto the triggering user's tile (Habbo's "move furni to user",
/// <c>wf_act_furni_to_user</c>, no int params). A move that fails placement validation is skipped.</summary>
[RoomObjectLogic("wf_act_tp_furni_to_habbo")]
public class WiredActionMoveFurniToUser(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_FURNI_TO_USER;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [WiredPlayerSourceType.TriggeredUser, WiredPlayerSourceType.SelectorUsers],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        if (!TryResolveTargetTile(selection, out int targetX, out int targetY))
        {
            return true;
        }

        foreach (int furniId in selection.SelectedFurniIds)
        {
            await TryMoveFurniAsync(ctx, furniId, targetX, targetY);
        }

        return true;
    }

    private bool TryResolveTargetTile(IWiredSelectionSet selection, out int x, out int y)
    {
        foreach (int playerId in selection.SelectedPlayerIds)
        {
            if (
                _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
                && _roomGrain._state.AvatarsByObjectId.TryGetValue(
                    objectId,
                    out IRoomAvatar? avatar
                )
            )
            {
                (x, y) = (avatar.X, avatar.Y);
                return true;
            }
        }

        (x, y) = (0, 0);
        return false;
    }

    private async Task TryMoveFurniAsync(
        IWiredExecutionContext ctx,
        int furniId,
        int targetX,
        int targetY
    )
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
            || item is not IRoomFloorItem floorItem
        )
        {
            return;
        }

        if (
            await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                ActionContext.Wired,
                floorItem.ObjectId,
                targetX,
                targetY,
                floorItem.Rotation
            )
        )
        {
            await ctx.ProcessFloorItemMovementAsync(
                floorItem,
                _roomGrain.MapModule.ToIdx(targetX, targetY),
                null,
                floorItem.Rotation
            );
        }
    }
}
