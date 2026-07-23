using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Teleports the selected users onto the tile of the selected furni (Habbo's teleport pad,
/// <c>wf_act_teleport_to</c>). Client MoveUserToFurni.ts carries a walk-mode toggle; this teleport
/// furni moves instantly (walk mode not yet honoured).</summary>
[RoomObjectLogic("wf_act_teleport_to")]
public class WiredActionTeleportToFurni(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.MOVE_USER_TO_FURNI;

    // Client MoveUserToFurni.ts: intParams = [walkMode] (0/1). Persisted; instant teleport for now.
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredBoolParamRule(false)];

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        if (!TryResolveDestinationTile(selection, out int tileIdx))
        {
            return true;
        }

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            if (TryResolveAvatar(playerId, out IRoomAvatar? avatar))
            {
                await ctx.ProcessUserMovementAsync(avatar, tileIdx, SlideAvatarMoveType.Move);
            }
        }

        return true;
    }

    private bool TryResolveDestinationTile(IWiredSelectionSet selection, out int tileIdx)
    {
        foreach (int furniId in selection.SelectedFurniIds)
        {
            if (
                _roomGrain._state.ItemsById.TryGetValue(furniId, out IRoomItem? item)
                && item is IRoomFloorItem floor
            )
            {
                tileIdx = _roomGrain.MapModule.ToIdx(floor.X, floor.Y);
                return true;
            }
        }

        tileIdx = 0;
        return false;
    }

    private bool TryResolveAvatar(int playerId, out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }
}
