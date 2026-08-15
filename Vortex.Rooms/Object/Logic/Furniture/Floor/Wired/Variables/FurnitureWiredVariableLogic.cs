using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains.Storage;
using Vortex.Rooms.Wired.Variables;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

public abstract class FurnitureWiredVariableLogic
    : FurnitureWiredLogic,
        IWiredVariable,
        IWiredVariableStore
{
    public override WiredType WiredType => WiredType.Variable;

    protected readonly WiredVariableId _variableId;

    protected virtual WiredVariableType VariableType => WiredVariableType.Created;
    protected abstract WiredVariableTargetType TargetType { get; }
    protected abstract WiredAvailabilityType AvailabilityType { get; }
    protected virtual WiredVariableFlags Flags => WiredVariableFlags.None;
    protected KeyValueStore? _storage = null;
    protected WiredVariableSnapshot? _varSnapshot;

    public FurnitureWiredVariableLogic(
        IGrainFactory grainFactory,
        IStuffDataFactory stuffDataFactory,
        IRoomFloorItemContext ctx
    )
        : base(grainFactory, stuffDataFactory, ctx)
    {
        _variableId = WiredVariableIdBuilder.CreateFromBoxId(ctx.ObjectId.Value);
    }

    public virtual bool CanBind(in WiredVariableKey key)
    {
        WiredVariableSnapshot snapshot = GetVarSnapshot();

        return key.VariableId == snapshot.VariableId && key.TargetType == snapshot.TargetType;
    }

    public virtual bool TryGetValue(in WiredVariableKey key, out WiredVariableValue value)
    {
        value = WiredVariableValue.Default;

        if (!CanBind(key) || !TryGetStore(key, out IWiredKeyValueStore? store) || store is null)
        {
            return false;
        }

        return store.TryGetValue(key, out value);
    }

    public virtual async Task<bool> GiveValueAsync(
        WiredVariableKey key,
        WiredVariableValue value,
        bool replace = false
    )
    {
        WiredVariableSnapshot snapshot = GetVarSnapshot();

        if (
            !snapshot.Flags.Has(WiredVariableFlags.CanCreateAndDelete)
            || !CanBind(key)
            || !TryGetStore(key, out IWiredKeyValueStore? store)
            || store is null
            || (store.ContainsKey(key) && !replace)
        )
        {
            return false;
        }

        bool existed = store.TryGetValue(key, out WiredVariableValue previous);

        if (!await store.GiveValueAsync(key, value, replace))
        {
            return false;
        }

        await PublishChangeAsync(
            key,
            existed ? WiredVariableChangeKind.ValueChanged : WiredVariableChangeKind.Created,
            existed ? previous.Value : 0,
            value.Value
        );

        return true;
    }

    public virtual async Task<bool> SetValueAsync(
        IWiredExecutionContext ctx,
        WiredVariableKey key,
        WiredVariableValue value
    )
    {
        if (
            !TryGetStore(key, out IWiredKeyValueStore? store)
            || store is null
            || !store.ContainsKey(key)
        )
        {
            return false;
        }

        store.TryGetValue(key, out WiredVariableValue previous);

        if (!await store.SetValueAsync(ctx, key, value))
        {
            return false;
        }

        await PublishChangeAsync(
            key,
            WiredVariableChangeKind.ValueChanged,
            previous.Value,
            value.Value
        );

        return true;
    }

    public virtual bool RemoveValue(WiredVariableKey key)
    {
        if (!TryGetStore(key, out IWiredKeyValueStore? store) || store is null)
        {
            return false;
        }

        store.TryGetValue(key, out WiredVariableValue previous);

        if (!store.RemoveValue(key))
        {
            return false;
        }

        // Removal is synchronous by contract (the whole variable store is, so a condition can read
        // it), so the event is observed rather than awaited.
        PublishChangeAsync(key, WiredVariableChangeKind.Deleted, previous.Value, 0)
            .LogAndForget(_logger, "Failed to publish a wired variable deletion.");

        return true;
    }

    public virtual bool TryGetTimestamps(
        in WiredVariableKey key,
        out long createdAtMs,
        out long updatedAtMs
    )
    {
        createdAtMs = 0;
        updatedAtMs = 0;

        return CanBind(key)
            && TryGetStore(key, out IWiredKeyValueStore? store)
            && store is not null
            && store.TryGetTimestamps(key, out createdAtMs, out updatedAtMs);
    }

    /// <summary>
    /// Tells the room a value moved, so the "variable changed" trigger can fire. This sits on the
    /// box rather than on the callers because every write reaches the store through here — a wired
    /// action, the wired menu's set-value, anything later — and a trigger that only saw one of
    /// those paths would look broken exactly when someone used the other.
    /// </summary>
    private Task PublishChangeAsync(
        WiredVariableKey key,
        WiredVariableChangeKind kind,
        int previous,
        int current
    ) =>
        _ctx.PublishRoomEventAsync(
            new WiredVariableChangedEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ActionContext.CreateForWired(_ctx.RoomId),
                Key = key,
                Kind = kind,
                Previous = previous,
                Current = current,
            },
            CancellationToken.None
        );

    public virtual Dictionary<WiredVariableValue, string> GetTextConnectors() => [];

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        _varSnapshot = null;

        await base.FillInternalDataAsync(ct);

        WiredVariableSnapshot snapshot = GetVarSnapshot();

        if (snapshot.AvailabilityType == WiredAvailabilityType.Persistent)
        {
            if (_storage == null)
            {
                if (
                    _ctx.RoomObject.ExtraData.TryGetSection(
                        ExtraDataSectionType.STORAGE,
                        out JsonElement storageElement
                    )
                )
                {
                    _storage = storageElement.Deserialize<KeyValueStore>();
                }
                else
                {
                    _storage = new();
                }

                _storage?.SetAction(() =>
                {
                    _ctx.RoomObject.ExtraData.UpdateSection(
                        ExtraDataSectionType.STORAGE,
                        JsonSerializer.SerializeToNode(_storage, _storage.GetType())
                    );
                    return Task.CompletedTask;
                });
            }
        }
    }

    private bool TryGetStore(WiredVariableKey key, out IWiredKeyValueStore? store)
    {
        if (_storage is not null)
        {
            store = _storage;

            return true;
        }

        return _ctx.Furni.TryGetVariableStore(key, out store);
    }

    public WiredVariableSnapshot GetVarSnapshot() => _varSnapshot ??= BuildVarSnapshot();

    protected virtual WiredVariableSnapshot BuildVarSnapshot()
    {
        Dictionary<WiredVariableValue, string> textConnectors = GetTextConnectors();
        WiredVariableHash variableHash = WiredVariableHashBuilder.HashValues(
            _wiredData.StringParam,
            AvailabilityType,
            TargetType,
            Flags,
            textConnectors
        );

        return new()
        {
            VariableId = _variableId,
            VariableName = _wiredData.StringParam,
            VariableType = WiredVariableType.Created,
            VariableHash = variableHash,
            AvailabilityType = AvailabilityType,
            TargetType = TargetType,
            Flags = Flags,
            TextConnectors = textConnectors,
        };
    }

    public override async Task OnPickupAsync(ActionContext ctx, CancellationToken ct)
    {
        _ctx.RoomObject.ExtraData.DeleteSection(ExtraDataSectionType.STORAGE);

        await base.OnPickupAsync(ctx, ct);
    }

    protected override Task OnWiredStackChangedAsync(
        ActionContext ctx,
        List<int> ids,
        CancellationToken ct
    ) =>
        _ctx.PublishRoomEventAsync(
            new WiredVariableBoxChangedEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ctx,
                BoxIds = [_ctx.ObjectId.Value],
            },
            ct
        );
}
