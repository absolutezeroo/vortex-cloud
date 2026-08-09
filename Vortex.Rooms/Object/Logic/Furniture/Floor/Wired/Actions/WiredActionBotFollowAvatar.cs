using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Habbo's "bot follows avatar" wired. The setup form is a name and a two-way radio — start or stop
/// following — written into int param [0], where <c>1</c> starts and <c>0</c> stops.
/// <para>
/// A bot follows one person at a time, so when a stack resolves several the first one wins. Telling
/// it to follow each in turn would leave it chasing whichever happened to be resolved last.
/// </para>
/// </summary>
[RoomObjectLogic("wf_act_bot_follow_avatar")]
public class WiredActionBotFollowAvatar(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    private const int StartFollowing = 1;

    public override int WiredCode => (int)WiredActionType.BOT_FOLLOW_AVATAR;

    public override List<IWiredParamRule> GetIntParamRules() =>
        [
            new WiredRangeParamRule(0, 1, 0), // 0 = stop following, 1 = start
        ];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        string botName = (_wiredData.StringParam ?? string.Empty).Trim();

        if (botName.Length == 0)
        {
            return true;
        }

        bool start =
            _wiredData.IntParams.Count > 0 && _wiredData.GetIntParam<int>(0) == StartFollowing;

        if (!start)
        {
            await ctx.ProcessBotFollowAsync(botName, null);

            return true;
        }

        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            await ctx.ProcessBotFollowAsync(botName, new PlayerId(playerId));

            return true;
        }

        return true;
    }
}
