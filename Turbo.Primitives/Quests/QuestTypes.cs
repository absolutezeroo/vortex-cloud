namespace Turbo.Primitives.Quests;

/// <summary>
/// Canonical quest objective type names (the <c>quest_type</c> column). A quest advances when a
/// progression trigger fires with the matching type. Kept in Turbo.Primitives so both the room/
/// packet handlers and the player-side quest handlers reference the same constants.
/// </summary>
public static class QuestTypes
{
    public const string RoomEntry = "RoomEntry";
    public const string FriendListSize = "FriendListSize";
    public const string AvatarLooks = "AvatarLooks";
    public const string Chat = "Chat";
    public const string Wave = "Wave";
    public const string Dance = "Dance";
    public const string Login = "Login";
    public const string GamePlayed = "GamePlayed";
    public const string RespectGiven = "RespectGiven";
}
