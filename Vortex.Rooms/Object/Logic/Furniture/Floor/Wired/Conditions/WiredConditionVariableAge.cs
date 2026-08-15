using System;
using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Conditions;

/// <summary>
/// "The variable is older / younger than ...". Completes the variable family: a room can now branch
/// on how long ago something happened — a cooldown that has expired, a badge granted this week —
/// without keeping a second variable holding a timestamp by hand.
/// </summary>
/// <remarks>
/// Six int params in the client's order: [0] the variable's source type, [1] the comparison (only
/// "Lower than" 0 and "Higher than" 2 are offered), [2] which moment to measure from (0 creation,
/// 1 last update), [3] and [4] the duration as a signed long pair, [5] the time unit. The client
/// disables whichever of the two moments the picked variable cannot report.
/// <para>
/// The age is wall-clock: stored values carry the Unix millisecond they were written at, because
/// the room clock restarts with the room and would make every value look newly created after a
/// reload. A value written before the room kept times has no stamp, and the condition fails rather
/// than treating it as brand new.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_cnd_var_age_match")]
public class WiredConditionVariableAge(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredVariableConditionLogic(grainFactory, stuffDataFactory, ctx)
{
    /// <summary>The client's "Compare value" radio: creation time, or last update time.</summary>
    private const int FromCreation = 0;

    public override int WiredCode => (int)WiredConditionType.VARIABLE_AGE;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredEnumParamRule<WiredVariableTargetType>(
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.Furni,
                WiredVariableTargetType.User,
                WiredVariableTargetType.Global,
                WiredVariableTargetType.Context
            ),
            // Only the two the form draws; a third would be a config this box cannot mean.
            new WiredEnumParamRule<WiredComparisonType>(
                WiredComparisonType.GreaterThan,
                WiredComparisonType.LessThan,
                WiredComparisonType.GreaterThan
            ),
            new WiredRangeParamRule(0, 1, FromCreation),
            new WiredParamRule(0),
            new WiredParamRule(0),
            new WiredEnumParamRule<WiredTimeUnit>(WiredTimeUnit.Seconds),
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

    public override bool Evaluate(IWiredProcessingContext ctx)
    {
        bool result = false;

        if (
            _wiredData.IntParams.Count >= 6
            && TryReadTimestamps(
                VariableIdAt(0),
                VariableTarget,
                out long createdAtMs,
                out long updatedAtMs
            )
        )
        {
            long writtenAtMs =
                _wiredData.GetIntParam<int>(2) == FromCreation ? createdAtMs : updatedAtMs;

            // A moment the store never recorded is unknown, not the epoch: measuring an age from
            // zero would make every such value 56 years old and fire every "older than" box.
            if (writtenAtMs > 0)
            {
                long durationMs = WiredVariableAge.ToMilliseconds(
                    WiredIntAsLong.ReadClamped(
                        _wiredData.GetIntParam<int>(3),
                        _wiredData.GetIntParam<int>(4)
                    ),
                    _wiredData.GetIntParam<WiredTimeUnit>(5)
                );

                result = WiredVariableAge.Matches(
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - writtenAtMs,
                    _wiredData.GetIntParam<WiredComparisonType>(1),
                    durationMs
                );
            }
        }

        return IsNegative() ? !result : result;
    }
}
