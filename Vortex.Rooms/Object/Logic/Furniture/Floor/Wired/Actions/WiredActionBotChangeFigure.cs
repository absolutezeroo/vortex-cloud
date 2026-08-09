using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Actions;

/// <summary>
/// Habbo's "change bot look" wired. Its form has a capture button that stamps a figure string into
/// the configuration when it is saved, so the look is fixed at setup time rather than read off
/// anybody when the stack runs — unlike the bot menu's own dress-up.
/// <para>
/// Same packing as the bot-talk wireds: <c>bot name \t figure</c>.
/// </para>
/// </summary>
[RoomObjectLogic("wf_act_bot_clothes")]
public class WiredActionBotChangeFigure(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredActionLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredActionType.BOT_CHANGE_FIGURE;

    public override async Task<bool> ExecuteAsync(IWiredExecutionContext ctx, CancellationToken ct)
    {
        (string botName, string figure) = WiredActionBotTalk.SplitConfiguration(
            _wiredData.StringParam
        );

        if (botName.Length == 0 || figure.Length == 0)
        {
            return true;
        }

        await ctx.ProcessBotFigureAsync(botName, figure);

        return true;
    }
}
