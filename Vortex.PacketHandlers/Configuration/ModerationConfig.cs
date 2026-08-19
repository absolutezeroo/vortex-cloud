namespace Vortex.PacketHandlers.Configuration;

/// <summary>
/// Config keys and defaults for moderation-tool limits, served live from <c>IServerConfigGrain</c>
/// (migrated off IOptions/appsettings). The default is the fallback when a key has no admin override
/// stored in the DB.
/// </summary>
public static class ModerationConfig
{
    /// <summary>
    /// The staff CFH tool's mute action carries no duration on the wire (unlike the in-room mute
    /// panel, which sends explicit minutes) — this is the server-side default applied for it.
    /// </summary>
    public const string ModToolDefaultMuteMinutesKey = "moderation.modtool_default_mute_minutes";
    public const int ModToolDefaultMuteMinutesDefault = 60;

    /// <summary>Max chat lines returned per room for GetRoomChatlogMessageHandler.</summary>
    public const string RoomChatlogLimitKey = "moderation.room_chatlog_limit";
    public const int RoomChatlogLimitDefault = 100;

    /// <summary>Max distinct rooms returned for GetUserChatlogMessageHandler.</summary>
    public const string UserChatlogRoomLimitKey = "moderation.user_chatlog_room_limit";
    public const int UserChatlogRoomLimitDefault = 10;

    /// <summary>Max chat lines per room for GetUserChatlogMessageHandler.</summary>
    public const string UserChatlogMessagesPerRoomKey = "moderation.user_chatlog_messages_per_room";
    public const int UserChatlogMessagesPerRoomDefault = 50;

    /// <summary>Max visit rows returned for GetRoomVisitsMessageHandler. The client's list has no
    /// date column, so a window much wider than a session is not useful to a moderator anyway.</summary>
    public const string RoomVisitLimitKey = "moderation.room_visit_limit";
    public const int RoomVisitLimitDefault = 50;

    /// <summary>How recently a player must have registered to answer the staff <c>:anew</c> /
    /// <c>:uc new</c> classification.</summary>
    public const string NewUserClassificationDaysKey = "moderation.new_user_classification_days";
    public const int NewUserClassificationDaysDefault = 7;

    /// <summary>Cap on how many players one <c>:uc hotel</c> sweep will classify. The room-scoped
    /// form is bounded by the room; the hotel-scoped one is bounded by nothing but this.</summary>
    public const string UserClassificationHotelLimitKey =
        "moderation.user_classification_hotel_limit";
    public const int UserClassificationHotelLimitDefault = 200;

    /// <summary>CFH topic used for selfie reports. The client shows no topic picker for those — it
    /// sends a single "report this selfie" — so the categorisation is a server-side choice.</summary>
    public const string SelfieReportTopicKey = "moderation.selfie_report_topic_id";
    public const int SelfieReportTopicDefault = 1;
}
