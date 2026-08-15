using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// "WIRED Text Add-on: Variable placeholder" — <c>$(name)</c> becomes a variable's value, which is
/// what lets a room say its own scores and counters out loud.
/// </summary>
/// <remarks>
/// Int params are [single/multiple, the variable's source type, numeric-or-text], and the string
/// param is the placeholder name plus, for multiple, its delimiter after a tab.
/// <para>
/// "Text" mode prints the variable's text connector for that value instead of the number — the
/// mapping a variable box carries so a 1/2/3 can read as "bronze/silver/gold". A value with no
/// connector falls back to the number, which is better than saying nothing.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_text_output_variable")]
public class WiredAddonVariablePlaceholder(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonTextPlaceholder(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.VARIABLE_PLACEHOLDER;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredBoolParamRule(false),
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Global,
                WiredVariableTargetType.Context
            ),
            new WiredBoolParamRule(false),
        ];

    public override int GetMaxVariableIds() => 1;

    public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
        [
            [
                WiredFurniSourceType.SelectedItems,
                WiredFurniSourceType.SelectorItems,
                WiredFurniSourceType.SignalItems,
                WiredFurniSourceType.TriggeredItem,
            ],
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override List<WiredVariableContextSnapshot> GetWiredContextSnapshots() =>
        [
            new WiredVariableAllInRoomSnapshot()
            {
                ContextType = WiredContextType.AllVariablesInRoom,
                AllVariablesHash = _ctx.Furni.AllVariablesHash,
            },
        ];

    protected override async Task<IReadOnlyList<string>> ResolveValuesAsync(
        IWiredExecutionContext ctx,
        CancellationToken ct
    )
    {
        List<string> values = [];

        if (_wiredData.IntParams.Count < 3 || _wiredData.VariableIds.Count == 0)
        {
            return values;
        }

        if (
            !WiredVariableAccess.TryResolve(
                _ctx.Furni,
                _wiredData.VariableIds[0],
                out WiredVariableId id,
                out IWiredVariable? variable
            )
        )
        {
            return values;
        }

        WiredVariableTargetType target = _wiredData.GetIntParam<WiredVariableTargetType>(1);
        bool textMode = _wiredData.GetIntParam<bool>(2);
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        Dictionary<WiredVariableValue, string> connectors = variable!
            .GetVarSnapshot()
            .TextConnectors;

        foreach (int targetId in WiredVariableAccess.TargetIds(target, selection))
        {
            if (
                !variable.TryGetValue(
                    new WiredVariableKey(id, target, targetId),
                    out WiredVariableValue value
                )
            )
            {
                continue;
            }

            values.Add(
                textMode && connectors.TryGetValue(value, out string? connected)
                    ? connected
                    : value.Value.ToString(CultureInfo.InvariantCulture)
            );
        }

        return values;
    }
}
