namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// How a wired stack makes a bot speak. The client's two bot-talk setup forms each offer a pair of
/// radio buttons — talk or shout for the room, talk or whisper for one avatar — and both write the
/// choice into the action's first int param, which is why they share one type here.
/// </summary>
public enum WiredBotChatType
{
    /// <summary>Ordinary room chat, which is what both forms call "talk".</summary>
    Say = 0,

    Shout = 1,

    /// <summary>Only the avatar the stack was triggered for hears it.</summary>
    Whisper = 2,
}
