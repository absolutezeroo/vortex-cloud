using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Wired.LevelUp;
using Vortex.Rooms.Wired.Rules;
using Vortex.Rooms.Wired.Variables;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>
/// Turns the variable it is stacked with into a level: the variable counts experience, this says
/// what level that is worth.
/// </summary>
/// <remarks>
/// The add-on holds no value of its own — it describes the box's variable, which is why it is an
/// add-on and not a variable box, and why it is the first implementer of
/// <see cref="IWiredSubVariableSource"/>, whose own documentation was written around this case.
/// <para>
/// The curve is chosen by param 1 and shaped by what follows it, so the param count is not fixed:
/// two for a manual curve (which lives in the string param), four for linear, five for exponential.
/// That is what the tail rule is for. Param 0 is a bitmask of which of the eight readings the
/// builder ticked; an unticked one is not created at all rather than created and hidden, because
/// the room's registry charges per variable.
/// </para>
/// <para>
/// Client: <c>roomevents/wired_setup/addons/VariableLevelUp.ts</c>, AIR's <c>_SafeCls_4387</c>.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_xtra_var_lvlup_system")]
public class WiredAddonVariableLevelUp(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredAddonLogic(grainFactory, stuffDataFactory, ctx), IWiredSubVariableSource
{
    private const int ModeManual = 0;

    private const int ModeLinear = 1;

    private const int ModeExponential = 2;

    /// <summary>The eight readings, in the client's own bit order, with the names it defaults to.</summary>
    /// <remarks>
    /// The names are not on the wire. The form shows a text input per row so the builder can rename
    /// one, and <c>VariableLevelUp.readIntParamsFromForm</c> sends only the mask — so what the
    /// server creates is always the default name, and that is the client's behaviour, not a
    /// shortcut. Renaming is a client-side label until the form starts sending it.
    /// <para>
    /// <c>Write</c> is the reading's inverse: what the experience has to become for the reading to
    /// answer the number asked for, given what it is now. Five of the eight have one, and the other
    /// three stay read-only because they have none — <c>xp_required</c> and <c>max_level</c> are
    /// statements about the curve rather than about the player, and "un-max someone" names no
    /// particular level.
    /// </para>
    /// <para>
    /// Every inverse ends in <see cref="WiredLevelUpCurve.BoundedValue"/>, which is what the reads
    /// already do, so a number past either end of the curve settles at the end instead of being
    /// refused. That is the curve's own convention, not a decision taken here.
    /// </para>
    /// </remarks>
    internal static readonly (
        string Name,
        Func<WiredLevelUpCurve, int, int> Read,
        Func<WiredLevelUpCurve, int, int, int>? Write
    )[] Readings =
    [
        (
            "current_level",
            static (curve, xp) => curve.CurrentLevel(xp),
            // The start of that level: the least experience that reads back as the level asked for.
            static (curve, _, level) => curve.XpForLevel(Math.Clamp(level, 1, curve.MaxLevel))
        ),
        (
            "current_xp",
            static (curve, xp) => curve.BoundedValue(xp),
            static (curve, _, xp) => curve.BoundedValue(xp)
        ),
        (
            "progress",
            static (curve, xp) => curve.Progress(xp),
            // Keeps the level and moves within it, so writing past what the level costs levels up.
            static (curve, xp, progress) =>
                curve.BoundedValue(curve.XpForLevel(curve.CurrentLevel(xp)) + progress)
        ),
        (
            "progress_percentage",
            static (curve, xp) => curve.ProgressPercentage(xp),
            static (curve, xp, percent) =>
                curve.BoundedValue(
                    curve.XpForLevel(curve.CurrentLevel(xp))
                        + (curve.TotalXpRequired(xp) * Math.Clamp(percent, 0, 100) / 100)
                )
        ),
        ("xp_required", static (curve, xp) => curve.TotalXpRequired(xp), null),
        (
            "xp_remaining",
            static (curve, xp) => curve.XpRemaining(xp),
            // Measured back from where the next level begins, which is what "still owed" means.
            static (curve, xp, remaining) =>
                curve.BoundedValue(curve.XpForLevel(curve.CurrentLevel(xp) + 1) - remaining)
        ),
        ("is_maxed", static (curve, xp) => curve.IsMaxed(xp) ? 1 : 0, null),
        ("max_level", static (curve, _) => curve.MaxLevel, null),
    ];

    private WiredLevelUpCurve? _curve;

    private int _mask;

    public override int WiredCode => (int)WiredAddonType.VARIABLE_LEVEL_UP;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, (1 << 8) - 1, 0), // 0 which readings to create
            new WiredRangeParamRule(ModeManual, ModeExponential, ModeLinear), // 1 curve mode
        ];

    /// <summary>
    /// The curve's own parameters, however many this mode takes.
    /// </summary>
    /// <remarks>
    /// A fixed list cannot express it: the client sends two ints for a manual curve, four for a
    /// linear one and five for an exponential one, and a box whose rule count does not match what
    /// the form sent rejects the whole save without a word. The values are range-checked where they
    /// are read instead, by the curve that knows what each of them means.
    /// </remarks>
    public override IWiredParamRule? GetIntParamTailRule() =>
        new WiredRangeParamRule(0, int.MaxValue, 0);

    public IReadOnlyList<IWiredVariable> CreateSubVariables(IWiredVariable parent)
    {
        if (_curve is null || _mask == 0)
        {
            return [];
        }

        string parentName = parent.GetVarSnapshot().VariableName;

        if (string.IsNullOrEmpty(parentName))
        {
            return [];
        }

        List<IWiredVariable> derived = [];

        for (int slot = 0; slot < Readings.Length; slot++)
        {
            if ((_mask & (1 << slot)) == 0)
            {
                continue;
            }

            (
                string name,
                Func<WiredLevelUpCurve, int, int> read,
                Func<WiredLevelUpCurve, int, int, int>? write
            ) = Readings[slot];

            derived.Add(
                new WiredLevelUpSubVariable(
                    parent,
                    WiredVariableIdBuilder.CreateFromBoxSubId(_ctx.ObjectId.Value, slot),
                    $"{parentName}.{name}",
                    _curve,
                    read,
                    write
                )
            );
        }

        return derived;
    }

    protected override async Task FillInternalDataAsync(CancellationToken ct)
    {
        await base.FillInternalDataAsync(ct);

        try
        {
            _mask = _wiredData.GetIntParam<int>(0);
            _curve = BuildCurve();
        }
        catch (Exception ex)
        {
            _mask = 0;
            _curve = null;

            _logger.LogWarning(
                ex,
                "Malformed level-up params for wired item {ItemId}; the add-on derives nothing.",
                _ctx.ObjectId
            );
        }
    }

    /// <summary>
    /// Null when the builder's curve does not describe one, and then the add-on derives nothing.
    /// </summary>
    /// <remarks>
    /// Only the manual mode can fail, and it fails the same way the client's preview does: a line
    /// whose level or XP does not exceed the line before it voids the whole curve rather than being
    /// skipped. Deriving levels from a shape the builder never saw previewed would be worse than
    /// deriving none.
    /// </remarks>
    private WiredLevelUpCurve? BuildCurve() =>
        _wiredData.GetIntParam<int>(1) switch
        {
            ModeLinear => new WiredLinearLevelUpCurve(Param(2, 100), Param(3, 50)),
            ModeExponential => new WiredExponentialLevelUpCurve(
                Param(2, 100),
                Param(3, 20),
                Param(4, 50)
            ),
            ModeManual => WiredInterpolatedLevelUpCurve.TryParse(_wiredData.StringParam),
            _ => null,
        };

    /// <summary>The form's own default when the slot is absent — a shorter list is a manual curve's
    /// list, not a broken one.</summary>
    private int Param(int index, int fallback) =>
        _wiredData.IntParams.Count > index ? _wiredData.GetIntParam<int>(index) : fallback;
}
