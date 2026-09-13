using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Wired;

public interface IWiredBox
{
    public WiredType WiredType { get; }
    public int WiredCode { get; }

    public Task LoadWiredAsync(CancellationToken ct);
    public Task FlashActivationStateAsync(CancellationToken ct);
    public List<int> GetStuffIds();
    public List<int> GetStuffIds2();

    /// <summary>The furni this box recorded the last time it was saved, for the boxes the client
    /// marks <c>hasStateSnapshot</c>. Empty for every other box, which is what makes
    /// <see cref="WiredFurniSourceType.SnapshotItems"/> select nothing on one.</summary>
    public List<int> GetSnapshotFurniIds();
    public List<IWiredParamRule> GetIntParamRules();
    public IWiredParamRule? GetIntParamTailRule();
    public List<WiredFurniSourceType[]> GetAllowedFurniSources();
    public List<WiredPlayerSourceType[]> GetAllowedPlayerSources();
    public List<Type> GetDefinitionSpecificTypes();
    public List<Type> GetTypeSpecificTypes();
    public List<WiredVariableContextSnapshot> GetWiredContextSnapshots();
    public List<WiredFurniSourceType[]> GetFurniSources();
    public List<WiredPlayerSourceType[]> GetPlayerSources();
    public List<WiredFurniSourceType[]> GetDefaultFurniSources();
    public List<WiredPlayerSourceType[]> GetDefaultPlayerSources();
    public Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        WiredUpdateRequest update,
        CancellationToken ct
    );
    public WiredDataSnapshot GetSnapshot();
}
