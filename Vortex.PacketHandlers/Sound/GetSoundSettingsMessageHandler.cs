using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.PacketHandlers.Preferences;
using Vortex.Protocol.Messages.Incoming.Sound;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Sound;

/// <summary>
/// Hands back the player's saved volumes. Unanswered, the three sliders sat at whatever the client
/// defaults to while the values the player had chosen stayed in the database — <c>SetSoundSettings</c>
/// has always persisted them, and nothing ever read them back on request.
/// </summary>
/// <remarks>
/// The reply is the account-preferences packet, not a sound-specific one: the client's sound manager
/// subscribes to that message and takes the volumes from it. There is no other packet to send.
/// </remarks>
public class GetSoundSettingsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetSoundSettingsMessage>
{
    public async ValueTask HandleAsync(
        GetSoundSettingsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await ctx.SendComposerAsync(
                await AccountPreferences
                    .BuildAsync(grainFactory.GetPlayerGrain(ctx.PlayerId), ct)
                    .ConfigureAwait(false),
                ct
            )
            .ConfigureAwait(false);
    }
}
