using System.Threading;
using System.Threading.Tasks;
using Vortex.Protocol.Messages.Outgoing.Preferences;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.PacketHandlers.Preferences;

/// <summary>
/// The account-preferences packet, built once for the several requests that answer with it.
/// </summary>
/// <remarks>
/// The client has no dedicated sound-settings packet: its sound manager listens for this one and
/// reads the three volumes out of it, so "give me my volumes" and "give me my preferences" are the
/// same answer. Building it in one place is what stops the two drifting — a field added to one
/// caller and not the other reaches the client only depending on which request it happened to make.
/// </remarks>
internal static class AccountPreferences
{
    public static async Task<AccountPreferencesEventMessageComposer> BuildAsync(
        IPlayerGrain player,
        CancellationToken ct
    )
    {
        PlayerWiredPreferencesSnapshot wiredPrefs = await player
            .GetWiredPreferencesAsync(ct)
            .ConfigureAwait(false);

        // In-memory read on the already-activated grain — no extra DB query.
        int preferedChatStyle = await player.GetChatStylePreferenceAsync(ct).ConfigureAwait(false);

        PlayerAccountPreferencesSnapshot accountPrefs = await player
            .GetAccountPreferencesAsync(ct)
            .ConfigureAwait(false);

        return new AccountPreferencesEventMessageComposer
        {
            UIVolume = accountPrefs.UiVolume,
            FurniVolume = accountPrefs.FurniVolume,
            TraxVolume = accountPrefs.TraxVolume,
            FreeFlowChatDisabled = accountPrefs.FreeFlowChatDisabled,
            RoomInvitesIgnored = accountPrefs.RoomInvitesIgnored,
            RoomCameraFollowDisabled = accountPrefs.RoomCameraFollowDisabled,
            UIFlags = (UIFlags)accountPrefs.UiFlags,
            PreferedChatStyle = preferedChatStyle,
            WiredMenuButton = wiredPrefs.WiredMenuButton,
            WiredInspectButton = wiredPrefs.WiredInspectButton,
            PlayTestMode = wiredPrefs.PlayTestMode,
            VariableSyntaxMode = 1,
            WiredWhisperDisabled = wiredPrefs.WiredWhisperDisabled,
            ShowAllNotifications = wiredPrefs.ShowAllNotifications,
            UiStyle = wiredPrefs.UiStyle,
        };
    }
}
