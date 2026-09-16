using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.Player;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// "The user performs an action" (trigger code 16): fires when the triggering player waves, blows a
/// kiss, laughs, sends a respect, sleeps, holds up a sign or dances — and, for a sign or a dance,
/// optionally only for the one the box names.
/// </summary>
/// <remarks>
/// <para>
/// This box was registered and deliberately inert, on the reading that the client shipped no
/// configuration class for code 16. That is no longer true — and the class is easy to miss, because
/// it is named for what it does rather than for its code: <c>wired_setup/triggerconfs/
/// UserPerformsAction.ts</c> (AS3 <c>_SafeCls_4035</c>, <c>TriggerConfCodes.TRIGGER_CODE_16</c>),
/// registered in <c>TriggerConfs.ts</c>. So the box can be configured, and now it fires.
/// </para>
/// <para>
/// The form writes the action code to <c>intParams[0]</c> and encodes the sign or dance index into
/// the string param — bare digits for a sign, <c>"dance N"</c> for a dance, empty for "any".
/// </para>
/// <para>
/// Postures (6 sit, 7 stand, 8 lay) and 4 awake are offered by the form but never published: see
/// <see cref="PlayerPerformedActionEvent"/> for why, and use the matching wired *condition* for
/// them, which reads them off the avatar.
/// </para>
/// </remarks>
[RoomObjectLogic("wf_trg_user_performs_action")]
// The hotel's own "user starts dancing" box, which has no form of its own: this is the box it
// wanted to be, with the dance picked in the dropdown rather than baked into the classname.
[RoomObjectLogic("wf_trg_starts_dancing")]
public class WiredTriggerHabboPerformsAction(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int SignActionCode = 10;

    private const int DanceActionCode = 11;

    /// <summary>The client's own prefix for a dance index (<c>WiredUserAction.as</c>).</summary>
    private const string DancePrefix = "dance ";

    public override int WiredCode => (int)WiredTriggerType.AVATAR_PERFORMS_ACTION;

    public override List<Type> SupportedEventTypes { get; } = [typeof(PlayerPerformedActionEvent)];

    // [0] = the client's WiredUserAction code (UserPerformsAction.readIntParamsFromForm). Free
    // rather than ranged: the catalogue is sparse (there is no 9, and it ends on 67), so a range
    // would reject codes the form can legitimately send.
    public override List<IWiredParamRule> GetIntParamRules() => [new WiredParamRule(0)];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not PlayerPerformedActionEvent evt || _wiredData.IntParams.Count == 0)
        {
            return Task.FromResult(false);
        }

        if (_wiredData.GetIntParam<int>(0) != evt.ActionCode)
        {
            return Task.FromResult(false);
        }

        int wanted = WantedExtra(evt.ActionCode, _wiredData.StringParam);

        if (wanted >= 0 && wanted != evt.Extra)
        {
            return Task.FromResult(false);
        }

        // The performer is the triggering user for downstream "triggered user" effects.
        ctx.Selected.SelectedPlayerIds.Add(evt.PlayerId);

        return Task.FromResult(true);
    }

    /// <summary>
    /// The sign or dance the box narrows to, or <c>-1</c> for "any" — which is what an empty string
    /// param means, and what every action without an index gets.
    /// </summary>
    private static int WantedExtra(int actionCode, string stringParam)
    {
        if (string.IsNullOrEmpty(stringParam))
        {
            return -1;
        }

        string digits = actionCode switch
        {
            SignActionCode => stringParam,
            DanceActionCode => stringParam.StartsWith(DancePrefix, StringComparison.Ordinal)
                ? stringParam[DancePrefix.Length..]
                : string.Empty,
            _ => string.Empty,
        };

        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            ? id
            : -1;
    }
}
