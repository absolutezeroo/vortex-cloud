using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Protocol.Messages.Incoming.Preferences;
using Vortex.Protocol.Messages.Outgoing.Preferences;

namespace Vortex.PacketHandlers.Preferences;

/// <summary>
/// Discord Rich Presence preferences: the two packets the client's Discord component sends, and the
/// one answer both get.
/// </summary>
/// <remarks>
/// <para>
/// The server's whole part in Rich Presence is storage. The client decides what to publish to
/// Discord and does the publishing itself; it only needs the four toggles back the way the player
/// left them, on every login, before it will open its own settings dialog at all.
/// </para>
/// <para>
/// Both handlers answer with the same composer, because the client has no separate "saved" reply —
/// it keeps whatever the last preferences event carried.
/// </para>
/// </remarks>
public class GetDiscordPreferencesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetDiscordPreferencesMessage>
{
    public async ValueTask HandleAsync(
        GetDiscordPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        PlayerAccountPreferencesSnapshot prefs = await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .GetAccountPreferencesAsync(ct)
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(DiscordPreferences.Build(prefs), ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Saves the four toggles. The reply is not an acknowledgement the client waits on — it already
/// applied the change locally — but sending it keeps the two sides agreed on the stored version.
/// </summary>
public class SetDiscordPreferencesMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<SetDiscordPreferencesMessage>
{
    public async ValueTask HandleAsync(
        SetDiscordPreferencesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerGrain(ctx.PlayerId)
            .SetDiscordPreferencesAsync(
                message.Version,
                message.ShowHabbo,
                message.ShareActivity,
                message.HideInHiddenRooms,
                message.AllowJoining,
                ct
            )
            .ConfigureAwait(false);

        await ctx.SendComposerAsync(
                new DiscordPreferencesEventMessageComposer
                {
                    Version = message.Version,
                    ShowHabbo = message.ShowHabbo,
                    ShareActivity = message.ShareActivity,
                    HideInHiddenRooms = message.HideInHiddenRooms,
                    AllowJoining = message.AllowJoining,
                },
                ct
            )
            .ConfigureAwait(false);
    }
}

internal static class DiscordPreferences
{
    public static DiscordPreferencesEventMessageComposer Build(
        PlayerAccountPreferencesSnapshot prefs
    ) =>
        new()
        {
            Version = prefs.DiscordSettingsVersion,
            ShowHabbo = prefs.DiscordShowHabbo,
            ShareActivity = prefs.DiscordShareActivity,
            HideInHiddenRooms = prefs.DiscordHideInHiddenRooms,
            AllowJoining = prefs.DiscordAllowJoining,
        };
}
