using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// Resolves the wired pile physically stacked on a tile, right now.
/// </summary>
/// <remarks>
/// <para>
/// Called at fire time, never cached. That is what makes the "same pile" rule free: a box dragged off
/// the tile or picked up is simply not in the result, so a trigger only ever drives the boxes stacked
/// with it — which is what Habbo does, and what a cached pile would have to be invalidated to
/// imitate.
/// </para>
/// <para>
/// Members come back ordered by object id, because effect execution has to be deterministic and the
/// physical stacking order is not meaningful in Habbo. The ordering is applied by the room view when
/// it materialises the tile.
/// </para>
/// </remarks>
internal sealed class WiredStackResolver(IWiredRoomView room, IWiredDiagnostics diagnostics)
{
    private readonly IWiredRoomView _room = room;
    private readonly IWiredDiagnostics _diagnostics = diagnostics;

    /// <summary>
    /// Classifies every co-located wired box into the trigger / selector / condition / addon / action
    /// buckets of a fresh stack.
    /// </summary>
    public async Task<WiredStack> BuildFromTileAsync(int tileIdx, CancellationToken ct)
    {
        WiredStack stack = new() { StackId = tileIdx };

        if (tileIdx < 0 || tileIdx >= _room.TileCount)
        {
            return stack;
        }

        foreach (IRoomFloorItem stackItem in _room.EnumerateTileFloorStack(tileIdx))
        {
            IRoomItem item = stackItem;

            if (
                item is null
                || item.Logic is not FurnitureWiredLogic wiredLogic
                // Variable boxes are wired furniture but not pile members: they are read by the
                // boxes that reference them, not run alongside them.
                || wiredLogic is FurnitureWiredVariableLogic
            )
            {
                continue;
            }

            try
            {
                await wiredLogic.LoadWiredAsync(ct);

                switch (wiredLogic)
                {
                    case FurnitureWiredTriggerLogic trigger:
                        stack.Triggers.Add(trigger);
                        break;
                    case FurnitureWiredSelectorLogic selector:
                        stack.Selectors.Add(selector);
                        break;
                    case FurnitureWiredConditionLogic condition:
                        stack.Conditions.Add(condition);
                        break;
                    case FurnitureWiredAddonLogic addon:
                        stack.Addons.Add(addon);
                        break;
                    case FurnitureWiredActionLogic effect:
                        stack.Actions.Add(effect);
                        break;
                }
            }
            catch (Exception ex)
            {
                // One box that will not hydrate costs the pile that box, not the pile.
                _diagnostics.Logger.LogWarning(
                    ex,
                    "Failed to load wired logic for item {ItemId} in room {RoomId}.",
                    item.ObjectId,
                    _room.RoomId
                );
            }
        }

        return stack;
    }

    /// <summary>
    /// Whether a box is still on the tile its pile was resolved from. Re-checked before a delayed
    /// effect runs: the pile was live truth when it was resolved, and a delay makes it history.
    /// </summary>
    public bool IsOnTile(RoomObjectId objectId, int tileIdx) => _room.IsOnTile(tileIdx, objectId);
}
