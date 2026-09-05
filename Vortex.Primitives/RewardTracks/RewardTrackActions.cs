namespace Vortex.Primitives.RewardTracks;

/// <summary>
/// The action codes a reward-track task can be defined on. A task names one of these in its
/// <c>action_code</c> column; the engine's event bridge raises the matching code when the
/// corresponding domain action has actually succeeded.
/// </summary>
/// <remarks>
/// <para>
/// These are content identifiers, not behaviour: nothing here is a class, a switch arm or a
/// handler. Adding a task to a campaign picks a code; adding a <em>new</em> code means there is a
/// new gameplay signal to bridge, and only then is there code to write — one event handler in
/// <c>RewardTrackEventHandlers</c>.
/// </para>
/// <para>
/// The client uses the same string as its artwork key
/// (<c>reward_track_tasks_&lt;actionType lowercased&gt;</c>) and as part of the task's localization
/// stem, which is why the official values are lower snake case. The ones marked below are the
/// action codes the official Introduction Track uses, taken from the client's own
/// <c>external_flash_texts</c>.
/// </para>
/// </remarks>
public static class RewardTrackActions
{
    /// <summary>Entered a room. Distinct-mode tasks count distinct rooms. (Introduction Track)</summary>
    public const string VisitRooms = "visit_rooms";

    /// <summary>Created a room. (Introduction Track)</summary>
    public const string CreateRoom = "create_room";

    /// <summary>Placed furniture in a room. (Introduction Track)</summary>
    public const string PlaceFurniture = "place_furniture";

    /// <summary>Moved furniture already in a room. (Introduction Track)</summary>
    public const string MoveFurniture = "move_furniture";

    /// <summary>Rotated furniture already in a room. (Introduction Track)</summary>
    public const string RotateFurniture = "rotate_furniture";

    /// <summary>Arrived somewhere through a teleport. (Introduction Track)</summary>
    public const string UseTeleport = "use_teleport";

    /// <summary>Said something a room accepted. (Introduction Track)</summary>
    public const string ChatWithUsers = "chat_with_users";

    /// <summary>Sent a friend request. (Introduction Track)</summary>
    public const string MakeFriends = "make_friends";

    /// <summary>Gave respect to another player. (Introduction Track)</summary>
    public const string GiveRespect = "give_respect";

    /// <summary>Sent a private message. (Introduction Track)</summary>
    public const string SendMessengerMessage = "send_messenger_message";

    /// <summary>Danced in a room. (Introduction Track)</summary>
    public const string DanceInRoom = "dance_in_room";

    /// <summary>Waved in a room. (Introduction Track)</summary>
    public const string WaveAtUser = "wave_at_user";

    /// <summary>Changed the avatar's figure. (Introduction Track)</summary>
    public const string ChangeOutfit = "change_outfit";

    /// <summary>Changed the profile motto. (Introduction Track)</summary>
    public const string ChangeMotto = "change_motto";

    /// <summary>Equipped a badge. (Introduction Track)</summary>
    public const string WearBadge = "wear_badge";

    /// <summary>Bought from the catalog. (Introduction Track)</summary>
    public const string BuyCatalogFurni = "buy_catalog_furni";

    /// <summary>Completed a trade.</summary>
    public const string CompleteTrade = "complete_trade";

    /// <summary>Spent currency. The amount is the signal's amount, so a task can require a total.</summary>
    public const string SpendCredits = "spend_credits";

    /// <summary>A pet gained a level. (Introduction Track)</summary>
    public const string LevelPet = "level_pet";

    /// <summary>Used a Habbicon. (Introduction Track)</summary>
    public const string UseHabbicon = "use_habbicon";

    /// <summary>Completed a Habbicon collection.</summary>
    public const string CompleteHabbiconCollection = "complete_habbicon_collection";

    /// <summary>Completed a quest.</summary>
    public const string CompleteQuest = "complete_quest";

    /// <summary>Levelled up an achievement.</summary>
    public const string AchievementLevel = "achievement_level";

    /// <summary>
    /// Raised by the wired <c>PROGRESS_REWARD_TRACK</c> action, which names its track and task
    /// directly rather than going through an action code. Present so content and the dashboard can
    /// describe such a task; the engine routes wired progress by id, not by this code.
    /// </summary>
    public const string Wired = "wired";
}
