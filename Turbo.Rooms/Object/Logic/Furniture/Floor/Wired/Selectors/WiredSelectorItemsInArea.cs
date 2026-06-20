using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Wired;
using Turbo.Rooms.Wired.Rules;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor.Wired.Selectors;

[RoomObjectLogic("wf_slc_furni_area")]
public class WiredSelectorItemsInArea(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredSelectorLogic(grainFactory, stuffDataFactory, ctx)
{
    private readonly HashSet<int> _tileIds = [];
    public override int WiredCode => (int)WiredSelectorType.FURNI_IN_AREA;

    public override List<IWiredParamRule> GetIntParamRules()
    {
        return
        [
            new WiredParamRule(0),
            new WiredParamRule(0),
            new WiredParamRule(0),
            new WiredParamRule(0)
        ];
    }

    public override Task<IWiredSelectionSet> SelectAsync(
        IWiredProcessingContext ctx,
        CancellationToken ct
    )
    {
        WiredSelectionSet output = new();

        foreach (int tileId in _tileIds)
        {
            try
            {
                HashSet<RoomObjectId> itemIds = _roomGrain._state.TileFloorStacks[tileId];

                foreach (RoomObjectId itemId in itemIds)
                {
                    output.SelectedFurniIds.Add(itemId);
                }
            }
            catch
            {
            }
        }

        return Task.FromResult((IWiredSelectionSet)output);
    }

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        _tileIds.Clear();

        await base.FillInternalDataAsync(ct);

        int rootX = _wiredData.GetIntParam<int>(0);
        int rootY = _wiredData.GetIntParam<int>(1);
        int areaW = _wiredData.GetIntParam<int>(2);
        int areaH = _wiredData.GetIntParam<int>(3);
        int mapW = _roomGrain.MapModule.Width;
        int mapH = _roomGrain.MapModule.Height;
        int size = 0;
        bool filled = false;

        for (int dy = 0; dy < areaH; dy++)
        {
            for (int dx = 0; dx < areaW; dx++)
            {
                if (size >= _roomGrain._roomConfig.WiredSelectorMaxAreaSize)
                {
                    filled = true;

                    break;
                }

                int x = rootX + dx;
                int y = rootY + dy;

                if (x >= mapW || y >= mapH)
                {
                    continue;
                }

                int tileId = y * mapW + x;

                _tileIds.Add(tileId);
                size++;
            }

            if (filled)
            {
                break;
            }
        }

        size = 0;
        filled = false;

        if (GetIsInvert())
        {
            HashSet<int> selectedTiles = _tileIds.ToHashSet();

            _tileIds.Clear();

            for (int y = 0; y < mapH; y++)
            {
                for (int x = 0; x < mapW; x++)
                {
                    if (size >= _roomGrain._roomConfig.WiredSelectorMaxAreaSize)
                    {
                        filled = true;

                        break;
                    }

                    int tileId = y * mapW + x;

                    if (selectedTiles.Contains(tileId))
                    {
                        continue;
                    }

                    _tileIds.Add(tileId);
                    size++;
                }

                if (filled)
                {
                    break;
                }
            }
        }
    }
}
