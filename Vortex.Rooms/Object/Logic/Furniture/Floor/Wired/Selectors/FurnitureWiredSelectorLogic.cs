using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

public abstract class FurnitureWiredSelectorLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredLogic(grainFactory, stuffDataFactory, ctx), IWiredSelector
{
    public override WiredType WiredType => WiredType.Selector;

    private bool _isFilter = false;
    private bool _isInvert = false;

    public override List<Type> GetDefinitionSpecificTypes() =>
        [.. base.GetDefinitionSpecificTypes(), typeof(bool), typeof(bool)];

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _isFilter = _wiredData.GetDefinitionParam<bool>(0);
            _isInvert = _wiredData.GetDefinitionParam<bool>(1);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Malformed selector params for wired item {ItemId}; keeping current defaults.",
                _ctx.ObjectId
            );
        }
    }

    public bool GetIsFilter() => _isFilter;

    public bool GetIsInvert() => _isInvert;

    /// <summary>Adds every player currently standing on <paramref name="tileId"/> to the selection.
    /// Shared by the user-position selectors (on-furni / in-area / in-neighborhood), which resolve the
    /// same tiles as their furni counterparts but collect avatars instead of items. Only real players
    /// are added — a selection set is addressed by player id, so pets and bots have nothing to add.</summary>
    protected void AddPlayersOnTile(int tileId, WiredSelectionSet output)
    {
        if (tileId < 0 || tileId >= _roomGrain._state.TileAvatarStacks.Length)
        {
            return;
        }

        foreach (RoomObjectId avatarId in _roomGrain._state.TileAvatarStacks[tileId])
        {
            if (
                _roomGrain._state.AvatarsByObjectId.TryGetValue(avatarId, out IRoomAvatar? avatar)
                && avatar is IRoomPlayer player
            )
            {
                output.SelectedPlayerIds.Add((int)player.PlayerId);
            }
        }
    }

    public virtual Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    ) => Task.FromResult<IWiredSelectionSet>(new WiredSelectionSet());
}
