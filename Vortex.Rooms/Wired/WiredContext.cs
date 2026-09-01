using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;

namespace Vortex.Rooms.Wired;

internal abstract class WiredContext(IWiredRoomHost host) : IWiredContext
{
    // Not exposed on IWiredContext. Wired boxes run inside the room's own activation, so handing
    // them the room would be handing them the whole room through a back door; everything they
    // legitimately need is a method on this context.
    //
    // The host rather than the grain: this base only ever reads the room, and reading it through the
    // view is what lets the engine above be built without a grain at all.
    protected readonly IWiredRoomHost _host = host;

    private IWiredRoomView Room => _host.View;

    public IWiredPolicy Policy { get; init; } = new WiredPolicy();
    public IWiredSelectionSet Selected { get; init; } = new WiredSelectionSet();
    public IWiredSelectionSet SelectorPool { get; init; } = new WiredSelectionSet();

    // The furni/users carried by the signal that fired this stack (empty unless a receive-signal
    // trigger fired it). Read by the SignalItems / SignalUsers sources and the from-signal selectors.
    public IWiredSelectionSet Signal { get; init; } = new WiredSelectionSet();
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

    public Task<bool> SetContextVariableAsync(string key, int value)
    {
        Variables[key] = value;

        return Task.FromResult(true);
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
                                    if (!Room.HasItem(id))
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
                                    if (!Room.HasItem(id))
                                    {
                                        continue;
                                    }

                                    set.SelectedFurniIds.Add(id);
                                }
                            }
                        }
                        break;
                    case WiredFurniSourceType.SignalItems:
                        set.SelectedFurniIds.UnionWith(Signal.SelectedFurniIds);
                        break;
                    case WiredFurniSourceType.AllRoomItems:
                        {
                            foreach (IRoomItem item in Room.AllItems())
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
                    case WiredPlayerSourceType.SignalUsers:
                        set.SelectedPlayerIds.UnionWith(Signal.SelectedPlayerIds);
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
                    case WiredFurniSourceType.SignalItems:
                        result.SelectedFurniIds.UnionWith(Signal.SelectedFurniIds);
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
                        result.SelectedPlayerIds.UnionWith(Signal.SelectedPlayerIds);
                        break;
                }
            }
        }

        TrimPlayers(result);

        return result;
    }

    /// <summary>
    /// Caps the players one action will act on. Every action and add-on takes its targets from this
    /// method, so this is the one place the bound has to be: three of them — give furni from a
    /// chest, give currency from a chest, offer a transaction — do a database round trip and a full
    /// inventory reload <em>per player</em>, sequentially, inside the room's turn. The furni half of
    /// a selection has been bounded since the beginning and the player half never was.
    /// </summary>
    /// <remarks>
    /// Trims rather than refuses: an action that gave a hundred people an effect and now gives it to
    /// the first hundred is the same action, while refusing outright would turn a busy room into a
    /// silently dead one. Which players survive the trim is unordered, and deliberately so — the
    /// limit sits above any room a hotel actually runs, so anything reaching it is a room built to
    /// hurt, and there is no fair order in which to serve it.
    /// </remarks>
    private void TrimPlayers(WiredSelectionSet result)
    {
        int limit = _host.View.Limits.WiredSelectedPlayersLimit;

        if (limit <= 0 || result.SelectedPlayerIds.Count <= limit)
        {
            return;
        }

        int[] kept = [.. result.SelectedPlayerIds.Take(limit)];

        result.SelectedPlayerIds.Clear();
        result.SelectedPlayerIds.UnionWith(kept);

        _host.Diagnostics.ChainStopped(WiredStopReason.SELECTION_LIMIT);
    }

    public virtual WiredContextSnapshot GetSnapshot() =>
        new()
        {
            Variables = new Dictionary<string, int>(Variables),
            Selected = Selected.GetSnapshot(),
        };
}
