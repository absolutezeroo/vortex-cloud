namespace Vortex.Primitives.Navigator.Enums;

/// <summary>
/// Who may perform a given moderation action in a room. Values are the client's, not ours: the
/// room-settings dialog builds its dropdowns from literal arrays — <c>[0,1,4,5]</c> for mute and
/// ban, <c>[0,1,2,4,5]</c> for kick (<c>RoomSettingsCtrl.as</c>). <see cref="All"/> is therefore
/// only ever reachable on the kick setting, but the parser casts whatever int arrives, so it has to
/// exist here for every action or the value falls through to "nobody".
/// </summary>
public enum ModSettingType
{
    Owner = 0,
    Rights = 1,

    /// <summary>Every occupant. Offered by the client on the kick dropdown only
    /// (<c>${navigator.roomsettings.moderation.all}</c> = "All users").</summary>
    All = 2,
    GroupRights = 4,
    RightsOrGroup = 5,
}
