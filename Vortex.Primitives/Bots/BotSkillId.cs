namespace Vortex.Primitives.Bots;

/// <summary>
/// The skill ids the client's bot menu speaks, read off its own RentableBotMenuView. The menu draws
/// a button only when the server has told it the bot carries that id — <c>botSkills.indexOf(3)</c>
/// for the walk button, and so on — so these numbers are the client's, not ours, and a bot whose
/// skill list the server never sends has no menu at all beyond picking it up.
/// </summary>
public static class BotSkillId
{
    /// <summary>The bot takes the look its owner is wearing right now.</summary>
    public const int DressUp = 1;

    /// <summary>Phrases, auto-chat and delay, configured through the client's chatter dialog.</summary>
    public const int Chatter = 2;

    /// <summary>Wandering. The button is a plain toggle: the client sends empty data every click.</summary>
    public const int RandomWalk = 3;

    /// <summary>Dancing, toggled the same way the walk button is.</summary>
    public const int Dance = 4;

    /// <summary>A rename, whose data is the new name typed into the client's dialog.</summary>
    public const int ChangeName = 5;

    /// <summary>
    /// Present in a skill list, this <em>hides</em> the pick-up button rather than adding anything —
    /// which is why it is named here to be avoided rather than sent.
    /// </summary>
    public const int NoPickUp = 12;
}
