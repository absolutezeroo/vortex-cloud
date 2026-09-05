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
/// THE STRING IS THE CLIENT'S ARTWORK KEY, AND THE CLIENT SETTLES IT. <c>RewardTrackTaskRowView</c>
/// builds the icon name as <c>"reward_track_tasks_" + actionType.toLowerCase()</c> and nothing else,
/// so a code that is not one of Habbo's own leaves the task with a blank square and a
/// <c>ResourceManager: Asset not found</c> warning. The vocabulary is therefore fixed by the thirty
/// <c>reward_track_tasks_*</c> embeds declared in the client's <c>HabboWindowManagerCom.as</c> — not
/// by <c>external_flash_texts</c>, which an earlier version of this file cited. That mistake is why
/// twelve of these codes used to be invented names (<c>visit_rooms</c>, <c>chat_with_users</c>,
/// <c>place_furniture</c>, …) and every task in the Introduction Track rendered without an icon.
/// </para>
/// <para>
/// <c>external_flash_texts</c> governs a different string: a task's <c>task_id</c>, which becomes
/// the localization stem <c>reward_track.&lt;track&gt;.task.&lt;task_id&gt;.name</c>. The two spaces
/// do not line up — the Introduction Track's <c>visit_rooms</c> task is driven by the
/// <c>enter_other_users_room</c> action — and a seed must not reuse one for the other.
/// </para>
/// <para>
/// The last six codes have no Habbo artwork at all: they are this hotel's own signals, and a task
/// defined on one shows an empty icon. That is deliberate — inventing a name would only move the
/// blank square, and borrowing an unrelated icon would lie about what the task is.
/// </para>
/// </remarks>
public static class RewardTrackActions
{
    /// <summary>Entered another player's room. Distinct-mode tasks count distinct rooms. (Introduction Track)</summary>
    public const string EnterOtherUsersRoom = "enter_other_users_room";

    /// <summary>Created a room. (Introduction Track)</summary>
    public const string CreateRoom = "create_room";

    /// <summary>Placed furniture in a room. (Introduction Track)</summary>
    public const string PlaceItem = "place_item";

    /// <summary>Moved furniture already in a room. (Introduction Track)</summary>
    public const string MoveItem = "move_item";

    /// <summary>Rotated furniture already in a room. (Introduction Track)</summary>
    public const string RotateItem = "rotate_item";

    /// <summary>
    /// Took a placed piece of furniture back into the inventory. No client artwork -- there is no
    /// <c>reward_track_tasks_pick_up_item</c> embed -- so this is usable as a later step of a
    /// sequence, where nothing is drawn, but makes a poor step 0.
    /// </summary>
    public const string PickUpItem = "pick_up_item";

    /// <summary>
    /// Stepped onto a piece of floor furniture. No client artwork, same caveat as
    /// <see cref="PickUpItem"/>. Fires often, which is why nothing is published unless some
    /// content is actually listening for it.
    /// </summary>
    public const string WalkOnFurni = "walk_on_furni";

    /// <summary>Arrived somewhere through a teleport. (Introduction Track)</summary>
    public const string Teleport = "teleport";

    /// <summary>Said something a room accepted. (Introduction Track)</summary>
    public const string ChatWithSomeone = "chat_with_someone";

    /// <summary>Sent a friend request. (Introduction Track)</summary>
    public const string RequestFriend = "request_friend";

    /// <summary>Gave respect to another player. (Introduction Track)</summary>
    public const string GiveRespect = "give_respect";

    /// <summary>Sent a private message. (Introduction Track)</summary>
    public const string SendMessengerMessage = "send_messenger_message";

    /// <summary>Danced in a room. (Introduction Track)</summary>
    public const string Dance = "dance";

    /// <summary>Waved in a room. (Introduction Track)</summary>
    public const string Wave = "wave";

    /// <summary>Changed the avatar's figure. (Introduction Track)</summary>
    public const string ChangeFigure = "change_figure";

    /// <summary>Changed the profile motto. (Introduction Track)</summary>
    public const string ChangeMotto = "change_motto";

    /// <summary>Equipped a badge. (Introduction Track)</summary>
    public const string WearBadge = "wear_badge";

    /// <summary>Bought from the catalog. (Introduction Track)</summary>
    public const string BuyFromCatalogue = "buy_from_catalogue";

    /// <summary>A pet gained a level. (Introduction Track)</summary>
    public const string PetLevel = "pet_level";

    /// <summary>Used a Habbicon. (Introduction Track)</summary>
    public const string UseHabbicon = "use_habbicon";

    // ---------------------------------------------------------------------------------------
    // No Habbo artwork exists for the codes below: a task defined on one draws an empty icon.
    // ---------------------------------------------------------------------------------------

    /// <summary>Completed a trade. No client artwork.</summary>
    public const string CompleteTrade = "complete_trade";

    /// <summary>
    /// Spent currency. The amount is the signal's amount, so a task can require a total. No client
    /// artwork.
    /// </summary>
    public const string SpendCredits = "spend_credits";

    /// <summary>Completed a Habbicon collection. No client artwork.</summary>
    public const string CompleteHabbiconCollection = "complete_habbicon_collection";

    /// <summary>Completed a quest. No client artwork.</summary>
    public const string CompleteQuest = "complete_quest";

    /// <summary>Levelled up an achievement. No client artwork.</summary>
    public const string AchievementLevel = "achievement_level";

    /// <summary>
    /// Raised by the wired <c>PROGRESS_REWARD_TRACK</c> action, which names its track and task
    /// directly rather than going through an action code. Present so content and the dashboard can
    /// describe such a task; the engine routes wired progress by id, not by this code. No client
    /// artwork.
    /// </summary>
    public const string Wired = "wired";
}
