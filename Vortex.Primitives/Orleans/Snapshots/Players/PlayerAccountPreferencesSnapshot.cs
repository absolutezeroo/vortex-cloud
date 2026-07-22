using Orleans;

namespace Vortex.Primitives.Orleans.Snapshots.Players;

/// <summary>
/// The player's persisted account preferences (everything the settings UI stores except the chat
/// bubble style, which lives on the player row). Surfaced to the client in the account-preferences
/// packet on login so the settings dialog reflects the saved selection.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PlayerAccountPreferencesSnapshot
{
    [Id(0)]
    public required int UiVolume { get; init; }

    [Id(1)]
    public required int FurniVolume { get; init; }

    [Id(2)]
    public required int TraxVolume { get; init; }

    [Id(3)]
    public required bool FreeFlowChatDisabled { get; init; }

    [Id(4)]
    public required bool RoomInvitesIgnored { get; init; }

    [Id(5)]
    public required bool RoomCameraFollowDisabled { get; init; }

    [Id(6)]
    public required int UiFlags { get; init; }
}
