using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Events.Bots;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;

/// <summary>
/// Habbo's "bot reaches habbo" trigger: fires when the named bot steps up beside somebody. Its
/// setup form asks for nothing but a name, so the bot is matched by that and the person reached
/// becomes the stack's triggered user.
/// </summary>
[RoomObjectLogic("wf_trg_bot_reached_avtr")]
public class WiredTriggerBotReachesHabbo(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureWiredTriggerLogic(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredTriggerType.BOT_AVATAR_REACHED;

    public override List<Type> SupportedEventTypes { get; } = [typeof(BotReachedAvatarEvent)];

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [WiredPlayerSourceType.BotByName, WiredPlayerSourceType.SelectorUsers],
        ];

    public override Task<bool> CanTriggerAsync(IWiredProcessingContext ctx, CancellationToken ct)
    {
        if (ctx.Event is not BotReachedAvatarEvent evt)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(MatchesConfiguredBot(evt.BotName));
    }

    /// <summary>
    /// The name typed into the form, matched the way the rest of the bot wireds match it. A blank
    /// form means any bot, which is what an unconfigured trigger has always meant elsewhere.
    /// </summary>
    protected bool MatchesConfiguredBot(string botName)
    {
        string configured = (_wiredData.StringParam ?? string.Empty).Trim();

        return configured.Length == 0
            || configured.Equals(botName, StringComparison.OrdinalIgnoreCase);
    }
}
