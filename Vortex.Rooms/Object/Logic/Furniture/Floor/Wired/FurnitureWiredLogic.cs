using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;
using Vortex.Primitives.Messages.Outgoing.Userdefinedroomevents;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;

public abstract class FurnitureWiredLogic(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx), IWiredBox
{
    protected readonly IGrainFactory _grainFactory = grainFactory;

    private WiredDataSnapshot? _snapshot;

    protected IWiredData _wiredData = null!;

    // Wired boxes hold durable player configuration (selected items, delays, conditions,
    // sources) and an enabled/disabled activation state. All of it must survive a room
    // unload / reboot, so a wired furni persists exactly like any other furni (Persistent),
    // not RoomActive. Only ephemeral runtime state — the per-tick "already triggered"
    // counters — stays in memory; that lives in the wired execution system and is never
    // serialized into WiredData, so it is correctly lost on unload by design.
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.Persistent;

    public abstract WiredType WiredType { get; }
    public abstract int WiredCode { get; }

    /// <summary>The room object id of this wired box, used by the wired system to skip a box that has been
    /// removed from the room (a stale trigger-registry entry) and reindex.</summary>
    public RoomObjectId ObjectId => _ctx.ObjectId;

    /// <summary>The tile this wired box currently sits on. The wired system resolves a trigger's pile live
    /// from this tile at fire time, so a box dragged to another tile drives that new tile's pile (and an
    /// empty tile fires nothing) — the "same pile" rule holds without any cached stack.</summary>
    public int TileIdx => _ctx.GetTileIdx();

    public async Task LoadWiredAsync(CancellationToken ct)
    {
        await FillInternalDataAsync(ct);
    }

    public Task FlashActivationStateAsync(CancellationToken ct)
    {
        return SetStateAsync(GetState() == 1 ? 0 : 1);
    }

    public virtual List<int> GetStuffIds()
    {
        if (GetValidStuffIds(_wiredData.StuffIds, out List<int>? stuffIds))
        {
            if (!_wiredData.StuffIds.SequenceEqual(stuffIds))
            {
                _wiredData.StuffIds = stuffIds;

                _wiredData.MarkDirty();
            }
        }

        return stuffIds ?? [];
    }

    public virtual List<int> GetStuffIds2()
    {
        if (GetValidStuffIds(_wiredData.StuffIds2, out List<int>? stuffIds))
        {
            if (!_wiredData.StuffIds2.SequenceEqual(stuffIds))
            {
                _wiredData.StuffIds2 = stuffIds;

                _wiredData.MarkDirty();
            }
        }

        return stuffIds ?? [];
    }

    public virtual List<IWiredParamRule> GetIntParamRules()
    {
        return [];
    }

    public virtual IWiredParamRule? GetIntParamTailRule()
    {
        return null;
    }

    public virtual List<WiredFurniSourceType[]> GetAllowedFurniSources()
    {
        return [];
    }

    public virtual List<WiredPlayerSourceType[]> GetAllowedPlayerSources()
    {
        return [];
    }

    public virtual List<Type> GetDefinitionSpecificTypes()
    {
        return [];
    }

    public virtual List<Type> GetTypeSpecificTypes()
    {
        return [];
    }

    public virtual List<WiredVariableContextSnapshot> GetWiredContextSnapshots()
    {
        return [];
    }

    public List<WiredFurniSourceType[]> GetFurniSources()
    {
        List<WiredFurniSourceType[]> sources = new();
        int index = 0;

        foreach (WiredFurniSourceType[] source in GetDefaultFurniSources())
        {
            WiredFurniSourceType[] sourceTypes = source;

            // Defaults yield one slot per allowed source; the persisted list only holds
            // slots the player has configured (often fewer, empty when unconfigured), so
            // fall back to the default whenever this slot has no persisted override.
            if (index < _wiredData.FurniSources.Count && _wiredData.FurniSources[index] is not null)
            {
                sourceTypes = _wiredData.FurniSources[index];
            }

            sources.Add(sourceTypes);
            index++;
        }

        return sources;
    }

    public List<WiredPlayerSourceType[]> GetPlayerSources()
    {
        List<WiredPlayerSourceType[]> sources = new();
        int index = 0;

        foreach (WiredPlayerSourceType[] source in GetDefaultPlayerSources())
        {
            WiredPlayerSourceType[] sourceTypes = source;

            // Defaults yield one slot per allowed source; the persisted list only holds
            // slots the player has configured (often fewer, empty when unconfigured), so
            // fall back to the default whenever this slot has no persisted override.
            if (
                index < _wiredData.PlayerSources.Count
                && _wiredData.PlayerSources[index] is not null
            )
            {
                sourceTypes = _wiredData.PlayerSources[index];
            }

            sources.Add(sourceTypes);
            index++;
        }

        return sources;
    }

    public List<WiredFurniSourceType[]> GetDefaultFurniSources()
    {
        return [.. GetAllowedFurniSources().Select(x => new[] { x[0] })];
    }

    public List<WiredPlayerSourceType[]> GetDefaultPlayerSources()
    {
        return [.. GetAllowedPlayerSources().Select(x => new[] { x[0] })];
    }

    public virtual async Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        UpdateWiredMessage update,
        CancellationToken ct
    )
    {
        try
        {
            // Guarantee the config is hydrated and the persistence callback is wired before
            // we mutate it. A client can send an update before the wired stack has been
            // processed (which is what normally triggers LoadWiredAsync); without this the
            // mutation would NRE on a null _wiredData, get swallowed, and the player's
            // configuration would be silently dropped instead of persisted.
            if (_wiredData is null)
            {
                await FillInternalDataAsync(ct);

                if (_wiredData is null)
                {
                    return false;
                }
            }

            List<int> intParams = new();
            string stringParam = update.StringParam;
            List<int> stuffIds = new();
            List<int> stuffIds2 = new();
            List<string> variableIds = new();
            List<WiredFurniSourceType[]> furniSources = new();
            List<WiredPlayerSourceType[]> playerSources = new();
            List<object> definitionSpecifics = new();
            List<object> typeSpecifics = new();

            if (TryNormalizeIntParams(update.IntParams, out List<int> normalizedIntParams))
            {
                intParams = normalizedIntParams;
            }
            else
            {
                return false;
            }

            if (GetValidStuffIds(update.StuffIds, out List<int> validStuffIds))
            {
                stuffIds = validStuffIds;
            }

            if (GetValidStuffIds(update.StuffIds2, out List<int> validStuffIds2))
            {
                stuffIds2 = validStuffIds2;
            }

            if (GetValidVariableIds(update.VariableIds, out List<WiredVariableId> validVariableIds))
            {
                variableIds = [.. validVariableIds.Select(x => x.ToString())];
            }

            int index = 0;
            List<WiredFurniSourceType[]> validFurniSources = GetAllowedFurniSources();

            foreach (WiredFurniSourceType[] source in GetDefaultFurniSources())
            {
                WiredFurniSourceType[]? sourceTypes = source;

                try
                {
                    if (update.FurniSources[index] is not null)
                    {
                        sourceTypes =
                        [
                            .. update
                                .FurniSources[index]
                                .Where(validFurniSources[index].Contains)
                                .Take(source.Length),
                        ];
                    }
                }
                catch (Exception ex)
                {
                    _roomGrain._logger.LogWarning(
                        ex,
                        "Malformed FurniSources[{Index}] in wired update for item {ItemId}; keeping default source.",
                        index,
                        _ctx.ObjectId
                    );
                }

                furniSources.Add(sourceTypes);
                index++;
            }

            index = 0;
            List<WiredPlayerSourceType[]> validPlayerSources = GetAllowedPlayerSources();

            foreach (WiredPlayerSourceType[] source in GetDefaultPlayerSources())
            {
                WiredPlayerSourceType[]? sourceTypes = source;

                try
                {
                    if (update.PlayerSources[index] is not null)
                    {
                        sourceTypes =
                        [
                            .. update
                                .PlayerSources[index]
                                .Where(validPlayerSources[index].Contains)
                                .Take(source.Length),
                        ];
                    }
                }
                catch (Exception ex)
                {
                    _roomGrain._logger.LogWarning(
                        ex,
                        "Malformed PlayerSources[{Index}] in wired update for item {ItemId}; keeping default source.",
                        index,
                        _ctx.ObjectId
                    );
                }

                playerSources.Add(sourceTypes);
                index++;
            }

            index = 0;

            foreach (Type specType in GetDefinitionSpecificTypes())
            {
                object specific = default!;

                try
                {
                    if (
                        update.DefinitionSpecifics[index] is not null
                        && specType.IsInstanceOfType(update.DefinitionSpecifics[index])
                    )
                    {
                        specific = update.DefinitionSpecifics[index];
                    }
                    else
                    {
                        specific = Activator.CreateInstance(specType)!;
                    }
                }
                catch (Exception ex)
                {
                    _roomGrain._logger.LogWarning(
                        ex,
                        "Failed to rehydrate definition-specific {Type} at index {Index} from wired update for item {ItemId}.",
                        specType,
                        index,
                        _ctx.ObjectId
                    );
                }

                definitionSpecifics.Add(specific);
                index++;
            }

            index = 0;

            foreach (Type specType in GetTypeSpecificTypes())
            {
                object specific = default!;

                try
                {
                    if (
                        update.TypeSpecifics[index] is not null
                        && specType.IsInstanceOfType(update.TypeSpecifics[index])
                    )
                    {
                        specific = update.TypeSpecifics[index];
                    }
                    else
                    {
                        specific = Activator.CreateInstance(specType)!;
                    }
                }
                catch (Exception ex)
                {
                    _roomGrain._logger.LogWarning(
                        ex,
                        "Failed to rehydrate type-specific {Type} at index {Index} from wired update for item {ItemId}.",
                        specType,
                        index,
                        _ctx.ObjectId
                    );
                }

                typeSpecifics.Add(specific);
                index++;
            }

            _wiredData.IntParams = intParams;
            _wiredData.StringParam = stringParam;
            _wiredData.StuffIds = stuffIds;
            _wiredData.StuffIds2 = stuffIds2;
            _wiredData.VariableIds = variableIds;
            _wiredData.FurniSources = furniSources;
            _wiredData.PlayerSources = playerSources;
            _wiredData.DefinitionSpecifics = definitionSpecifics;
            _wiredData.TypeSpecifics = typeSpecifics;

            _wiredData.MarkDirty();

            await OnWiredStackChangedAsync(ctx, [_ctx.GetTileIdx()], ct);

            return true;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to apply wired update for item {ItemId}.",
                _ctx.ObjectId
            );
            return false;
        }
    }

    public WiredDataSnapshot GetSnapshot()
    {
        return _snapshot ??= BuildSnapshot();
    }

    public virtual int GetMaxVariableIds()
    {
        return 0;
    }

    public virtual bool SupportsAdvancedMode()
    {
        return true;
    }

    public List<object> GetDefinitionSpecifics()
    {
        List<object> specifics = new();
        int index = 0;

        foreach (Type specType in GetDefinitionSpecificTypes())
        {
            object specific = null!;

            try
            {
                if (
                    index < _wiredData.DefinitionSpecifics.Count
                    && _wiredData.DefinitionSpecifics[index] is not null
                    && specType.IsInstanceOfType(_wiredData.DefinitionSpecifics[index])
                )
                {
                    specific = _wiredData.DefinitionSpecifics[index];
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Malformed persisted DefinitionSpecifics[{Index}] ({Type}) for wired item {ItemId}; falling back to a fresh instance.",
                    index,
                    specType,
                    _ctx.ObjectId
                );
            }

            specific ??= Activator.CreateInstance(specType)!;

            specifics.Add(specific);
            index++;
        }

        return specifics;
    }

    public List<object> GetTypeSpecifics()
    {
        List<object> specifics = new();
        int index = 0;

        foreach (Type specType in GetTypeSpecificTypes())
        {
            object specific = null!;

            try
            {
                if (
                    index < _wiredData.TypeSpecifics.Count
                    && _wiredData.TypeSpecifics[index] is not null
                    && specType.IsInstanceOfType(_wiredData.TypeSpecifics[index])
                )
                {
                    specific = _wiredData.TypeSpecifics[index];
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Malformed persisted TypeSpecifics[{Index}] ({Type}) for wired item {ItemId}; falling back to a fresh instance.",
                    index,
                    specType,
                    _ctx.ObjectId
                );
            }

            specific ??= Activator.CreateInstance(specType)!;

            specifics.Add(specific);
            index++;
        }

        return specifics;
    }

    public List<int> GetDefaultIntParams()
    {
        List<int> ints = new();

        foreach (IWiredParamRule rule in GetIntParamRules())
        {
            ints.Add(rule.DefaultValue);
        }

        return ints;
    }

    protected virtual bool TryNormalizeIntParams(List<int> proposed, out List<int> normalized)
    {
        normalized = [];

        List<IWiredParamRule> fixedRules = GetIntParamRules();
        IWiredParamRule? tailRule = GetIntParamTailRule();
        int min = fixedRules.Count;
        int max = Math.Max(min, _roomGrain._roomConfig.WiredMaxIntParams);

        if (proposed.Count > max)
        {
            return false;
        }

        if (tailRule is null)
        {
            if (proposed.Count != fixedRules.Count)
            {
                return false;
            }

            for (int i = 0; i < fixedRules.Count; i++)
            {
                IWiredParamRule rule = fixedRules[i];

                try
                {
                    int v = proposed[i];

                    if (!rule.IsValid(v))
                    {
                        return false;
                    }

                    normalized.Add(rule.Sanitize(v));
                }
                catch
                {
                    normalized.Add(fixedRules[i].DefaultValue);
                }
            }

            return true;
        }

        if (proposed.Count < min)
        {
            return false;
        }

        for (int i = 0; i < fixedRules.Count; i++)
        {
            IWiredParamRule rule = fixedRules[i];
            int v = i < proposed.Count ? proposed[i] : rule.DefaultValue;

            if (i < proposed.Count && !rule.IsValid(v))
            {
                return false;
            }

            normalized.Add(rule.Sanitize(v));
        }

        for (int i = fixedRules.Count; i < proposed.Count; i++)
        {
            int v = proposed[i];

            if (!tailRule.IsValid(v))
            {
                return false;
            }

            normalized.Add(tailRule.Sanitize(v));
        }

        return true;
    }

    protected virtual bool GetValidStuffIds(List<int> proposed, out List<int> stuffIds)
    {
        stuffIds = [];

        int count = 0;

        foreach (int id in proposed)
        {
            if (!_roomGrain._state.ItemsById.TryGetValue(id, out IRoomItem? item))
            {
                continue;
            }

            stuffIds.Add(id);

            count++;

            if (count >= _roomGrain._roomConfig.WiredSelectedItemsLimit)
            {
                break;
            }
        }

        return true;
    }

    protected virtual bool GetValidVariableIds(
        List<string> proposed,
        out List<WiredVariableId> variableIds
    )
    {
        variableIds = [];

        int count = 0;
        int max = GetMaxVariableIds();

        foreach (string id in proposed)
        {
            try
            {
                WiredVariableId variableId = WiredVariableId.Parse(id);
                IWiredVariable? variable = _roomGrain.WiredSystem.GetVariableById(variableId);

                if (variable is null)
                {
                    continue;
                }

                variableIds.Add(variableId);

                count++;

                if (count >= max)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Malformed variable id {VariableId} for wired item {ItemId}; skipping it.",
                    id,
                    _ctx.ObjectId
                );
            }
        }

        return true;
    }

    protected virtual Task FillInternalDataAsync(CancellationToken ct)
    {
        _snapshot = null;

        if (_wiredData is null)
        {
            if (
                _ctx.RoomObject.ExtraData.TryGetSection(
                    ExtraDataSectionType.WIRED,
                    out JsonElement wiredDataElement
                )
            )
            {
                _wiredData = wiredDataElement.Deserialize<WiredData>() ?? new WiredData();
            }
            else
            {
                _wiredData = new WiredData();
            }

            _wiredData.AttatchRules(GetIntParamRules());
        }

        if (TryNormalizeIntParams(_wiredData.IntParams, out List<int> normalizedIntParams))
        {
            if (!_wiredData.IntParams.SequenceEqual(normalizedIntParams))
            {
                _wiredData.IntParams = normalizedIntParams;
                _wiredData.MarkDirty();
            }
        }

        if (GetValidStuffIds(_wiredData.StuffIds, out List<int> stuffIds))
        {
            if (!_wiredData.StuffIds.SequenceEqual(stuffIds))
            {
                _wiredData.StuffIds = stuffIds;

                _wiredData.MarkDirty();
            }
        }

        if (GetValidStuffIds(_wiredData.StuffIds2, out List<int> stuffIds2))
        {
            if (!_wiredData.StuffIds2.SequenceEqual(stuffIds2))
            {
                _wiredData.StuffIds2 = stuffIds2;

                _wiredData.MarkDirty();
            }
        }

        if (GetValidVariableIds(_wiredData.VariableIds, out List<WiredVariableId> variableIds))
        {
            List<string> variableIdStrings = variableIds.Select(x => x.ToString()).ToList();

            if (!_wiredData.VariableIds.SequenceEqual(variableIdStrings))
            {
                _wiredData.VariableIds = variableIdStrings;

                _wiredData.MarkDirty();
            }
        }

        // Ensure the definition/type-specific slots exist and are correctly sized before any leaf
        // reads them in its own FillInternalDataAsync (conditions/selectors read the quantifier +
        // invert flags, actions read the delay). A freshly placed, never-configured box otherwise has
        // empty lists and GetDefinitionParam(0) throws IndexOutOfRange. GetDefinitionSpecifics /
        // GetTypeSpecifics preserve any valid persisted values and fill defaults for the rest.
        _wiredData.DefinitionSpecifics = GetDefinitionSpecifics();
        _wiredData.TypeSpecifics = GetTypeSpecifics();

        _wiredData.SetAction(() =>
        {
            _ctx.RoomObject.ExtraData.UpdateSection(
                ExtraDataSectionType.WIRED,
                JsonSerializer.SerializeToNode(_wiredData, _wiredData.GetType())
            );

            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    protected virtual WiredDataSnapshot BuildSnapshot()
    {
        return new WiredDataSnapshot
        {
            WiredType = WiredType,
            FurniLimit = _roomGrain._roomConfig.WiredSelectedItemsLimit,
            StuffIds = GetValidStuffIds(_wiredData.StuffIds, out List<int> validStuffIds)
                ? validStuffIds
                : [],
            StuffIds2 = GetValidStuffIds(_wiredData.StuffIds2, out List<int> validStuffIds2)
                ? validStuffIds2
                : [],
            StuffTypeId = _ctx.Definition.SpriteId,
            Id = _ctx.ObjectId,
            StringParam = _wiredData.StringParam,
            IntParams = _wiredData.IntParams,
            VariableIds = GetValidVariableIds(
                _wiredData.VariableIds,
                out List<WiredVariableId> validVariableIds
            )
                ? validVariableIds
                : [],
            FurniSourceTypes = GetFurniSources(),
            PlayerSourceTypes = GetPlayerSources(),
            Code = WiredCode,
            AdvancedMode = SupportsAdvancedMode(),
            AmountFurniSelections = [],
            AllowWallFurni = _roomGrain._roomConfig.WiredAllowWallFurni,
            AllowedFurniSources = GetAllowedFurniSources(),
            AllowedPlayerSources = GetAllowedPlayerSources(),
            DefaultFurniSources = GetDefaultFurniSources(),
            DefaultPlayerSources = GetDefaultPlayerSources(),
            DefinitionSpecifics = GetDefinitionSpecifics(),
            TypeSpecifics = GetTypeSpecifics(),
            ContextSnapshots = GetWiredContextSnapshots(),
            DefaultIntParams = GetDefaultIntParams(),
        };
    }

    public override async Task OnAttachAsync(CancellationToken ct)
    {
        await base.OnAttachAsync(ct);

        await OnWiredStackChangedAsync(
            ActionContext.CreateForSystem(_roomGrain.RoomId),
            [_ctx.GetTileIdx()],
            ct
        );
    }

    public override async Task OnDetachAsync(CancellationToken ct)
    {
        await base.OnDetachAsync(ct);

        await OnWiredStackChangedAsync(
            ActionContext.CreateForSystem(_roomGrain.RoomId),
            [_ctx.GetTileIdx()],
            ct
        );
    }

    public override Task OnStateChangedAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override async Task OnMoveAsync(ActionContext ctx, int prevIdx, CancellationToken ct)
    {
        await base.OnMoveAsync(ctx, prevIdx, ct);

        await OnWiredStackChangedAsync(ctx, [_ctx.GetTileIdx(), prevIdx], ct);
    }

    public override async Task OnPickupAsync(ActionContext ctx, CancellationToken ct)
    {
        await base.OnPickupAsync(ctx, ct);

        _ctx.RoomObject.ExtraData.DeleteSection(ExtraDataSectionType.WIRED);

        await OnWiredStackChangedAsync(ctx, [_ctx.GetTileIdx()], ct);
    }

    public override Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        _ = _grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new OpenEventMessageComposer { ItemId = _ctx.ObjectId })
            .ConfigureAwait(false);

        return Task.CompletedTask;
    }

    protected virtual Task OnWiredStackChangedAsync(
        ActionContext ctx,
        List<int> ids,
        CancellationToken ct
    )
    {
        return _ctx.PublishRoomEventAsync(
            new RoomWiredStackChangedEvent
            {
                RoomId = _ctx.RoomId,
                CausedBy = ctx,
                StackIds = ids,
            },
            ct
        );
    }
}
