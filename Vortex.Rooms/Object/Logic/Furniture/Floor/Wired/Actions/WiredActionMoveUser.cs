using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>Nudges the selected users one tile in a direction and/or rotates them (Habbo's "move user",
/// <c>wf_act_forward_user</c>). Client MoveUser.ts: intParams = [move, rotate], where move is -1 (no
/// move) or a direction 0-7, and rotate is -1 (no rotate), a direction 0-7, 9 (clockwise) or 10
/// (counter-clockwise).</summary>
[RoomObjectLogic("wf_act_forward_user")]
public class WiredActionMoveUser(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int NoChange = -1;
    private const int RotateClockwise = 9;
    private const int RotateCounterClockwise = 10;
    private const int DirectionCount = 8;

    public override int WiredCode => (int)WiredActionType.MOVE_USER;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [new WiredRangeParamRule(-1, 7, -1), new WiredRangeParamRule(-1, 10, -1)];

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
        int move = _wiredData.IntParams.Count > 0 ? _wiredData.GetIntParam<int>(0) : NoChange;
        int rotate = _wiredData.IntParams.Count > 1 ? _wiredData.GetIntParam<int>(1) : NoChange;

        if (move == NoChange && rotate == NoChange)
        {
            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            if (!TryResolveAvatar(playerId, out IRoomAvatar? avatar))
            {
                continue;
            }

            if (move != NoChange)
            {
                await TryMoveAsync(ctx, avatar, (Rotation)move);
            }

            if (rotate != NoChange)
            {
                Rotation newRotation = rotate switch
                {
                    RotateClockwise => (Rotation)(((int)avatar.Rotation + 1) % DirectionCount),
                    RotateCounterClockwise => (Rotation)(
                        ((int)avatar.Rotation + DirectionCount - 1) % DirectionCount
                    ),
                    _ => (Rotation)rotate,
                };

                await ctx.ProcessUserDirectionAsync(avatar, newRotation, newRotation);
            }
        }

        return true;
    }

    private async Task TryMoveAsync(
        IWiredExecutionContext ctx,
        IRoomAvatar avatar,
        Rotation direction
    )
    {
        int fromIdx = _roomGrain.MapModule.ToIdx(avatar.X, avatar.Y);

        if (
            _roomGrain.MapModule.TryGetTileInFront(fromIdx, direction, out int destIdx)
            && _roomGrain.MapModule.CanAvatarWalk(avatar, destIdx)
        )
        {
            await ctx.ProcessUserMovementAsync(avatar, destIdx, SlideAvatarMoveType.Move);
        }
    }

    private bool TryResolveAvatar(int playerId, out IRoomAvatar? avatar)
    {
        avatar = null;

        return _roomGrain._state.AvatarsByPlayerId.TryGetValue(playerId, out RoomObjectId objectId)
            && _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out avatar);
    }
}
