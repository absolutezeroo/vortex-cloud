using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Variables;

[RoomObjectLogic("wf_var_room")]
public class WiredVariableRoom(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredVariableBoxType.Global;

    protected override WiredVariableTargetType TargetType => WiredVariableTargetType.Global;

    protected override WiredAvailabilityType AvailabilityType =>
        _wiredData.GetIntParam<WiredAvailabilityType>(0);

    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.CanInterceptChanges
        | WiredVariableFlags.AlwaysAvailable
        | WiredVariableFlags.CanReadLastUpdateTime;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<WiredAvailabilityType>(
                WiredAvailabilityType.RoomActive,
                WiredAvailabilityType.RoomActive,
                WiredAvailabilityType.Persistent,
                WiredAvailabilityType.Shared
            ),
        ];

    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots()
    {
        WiredVariableSnapshot snapshot = GetVarSnapshot();

        return
        [
            new WiredVariableInfoAndValueSnapshot()
            {
                ContextType = WiredContextType.VariableInfoAndValue,
                Variable = snapshot,
                Value = TryGetValue(
                    new WiredVariableKey(snapshot.VariableId, snapshot.TargetType, 0),
                    out WiredVariableValue value
                )
                    ? value
                    : WiredVariableValue.Default,
            },
        ];
    }
}
