using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired;

public abstract class WiredContext(RoomGrain roomGrain) : IWiredContext
{
    protected RoomGrain _roomGrain = roomGrain;

    public IRoomGrain Room => _roomGrain;

    public IWiredPolicy Policy { get; init; } = new WiredPolicy();
    public IWiredSelectionSet Selected { get; init; } = new WiredSelectionSet();
    public IWiredSelectionSet SelectorPool { get; init; } = new WiredSelectionSet();
    public Dictionary<string, int> Variables { get; init; } = [];
    public CancellationToken CancellationToken { get; init; }

    public bool TryGetContextVariable(string key, out int value)
    {
        if (Variables.TryGetValue(key, out int intValue))
        {
            value = intValue;

            return true;
        }

        value = default;

        return false;
    }

    public async Task<bool> SetContextVariableAsync(string key, int value)
    {
        Variables[key] = value;

        return await Task.FromResult(true);
    }

    public Task<IWiredSelectionSet> GetWiredSelectionSetAsync(IWiredBox wired, CancellationToken ct)
    {
        WiredSelectionSet set = new WiredSelectionSet();

        foreach (WiredFurniSourceType[] source in wired.GetFurniSources())
        {
            foreach (WiredFurniSourceType sourceType in source)
            {
                switch (sourceType)
                {
                    case WiredFurniSourceType.TriggeredItem:
                        set.SelectedFurniIds.UnionWith(Selected.SelectedFurniIds);
                        break;
                    case WiredFurniSourceType.SelectedItems:
                        {
                            List<int>? stuffIds = wired.GetStuffIds();

                            if (stuffIds is not null && stuffIds.Count > 0)
                            {
                                foreach (int id in stuffIds)
                                {
                                    if (!_roomGrain._state.ItemsById.ContainsKey(id))
                                    {
                                        continue;
                                    }

                                    set.SelectedFurniIds.Add(id);
                                }
                            }

                            List<int>? stuffIds2 = wired.GetStuffIds2();

                            if (stuffIds2 is not null && stuffIds2.Count > 0)
                            {
                                foreach (int id in stuffIds2)
                                {
                                    if (!_roomGrain._state.ItemsById.ContainsKey(id))
                                    {
                                        continue;
                                    }

                                    set.SelectedFurniIds.Add(id);
                                }
                            }
                        }
                        break;
                    case WiredFurniSourceType.AllRoomItems:
                        {
                            foreach (IRoomItem item in _roomGrain._state.ItemsById.Values)
                            {
                                set.SelectedFurniIds.Add((int)item.ObjectId);
                            }
                        }
                        break;
                }
            }
        }

        foreach (WiredPlayerSourceType[] source in wired.GetPlayerSources())
        {
            foreach (WiredPlayerSourceType sourceType in source)
            {
                switch (sourceType)
                {
                    case WiredPlayerSourceType.TriggeredUser:
                        set.SelectedPlayerIds.UnionWith(Selected.SelectedPlayerIds);
                        break;
                }
            }
        }

        return Task.FromResult<IWiredSelectionSet>(set);
    }

    public async Task<IWiredSelectionSet> GetEffectiveSelectionAsync(
        IWiredBox wired,
        CancellationToken ct
    )
    {
        WiredSelectionSet result = new WiredSelectionSet();
        IWiredSelectionSet set = await GetWiredSelectionSetAsync(wired, ct);

        foreach (WiredFurniSourceType[] source in wired.GetFurniSources())
        {
            foreach (WiredFurniSourceType sourceType in source)
            {
                switch (sourceType)
                {
                    case WiredFurniSourceType.SelectedItems:
                        result.SelectedFurniIds.UnionWith(set.SelectedFurniIds);
                        break;
                    case WiredFurniSourceType.SelectorItems:
                        result.SelectedFurniIds.UnionWith(SelectorPool.SelectedFurniIds);
                        break;
                    case WiredFurniSourceType.TriggeredItem:
                        result.SelectedFurniIds.UnionWith(Selected.SelectedFurniIds);
                        break;
                }
            }
        }

        foreach (WiredPlayerSourceType[] source in wired.GetPlayerSources())
        {
            foreach (WiredPlayerSourceType sourceType in source)
            {
                switch (sourceType)
                {
                    case WiredPlayerSourceType.TriggeredUser:
                        result.SelectedPlayerIds.UnionWith(Selected.SelectedPlayerIds);
                        break;
                    case WiredPlayerSourceType.SelectorUsers:
                        result.SelectedPlayerIds.UnionWith(SelectorPool.SelectedPlayerIds);
                        break;
                    case WiredPlayerSourceType.SignalUsers:
                        break;
                }
            }
        }

        return result;
    }

    public virtual WiredContextSnapshot GetSnapshot() =>
        new()
        {
            Variables = new Dictionary<string, int>(Variables),
            Selected = Selected.GetSnapshot(),
        };
}
