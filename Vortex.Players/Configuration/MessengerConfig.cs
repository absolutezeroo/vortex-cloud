namespace Vortex.Players.Configuration;

public sealed class MessengerConfig
{
    public const string SECTION_NAME = "Vortex:Messenger";

    /// <summary>
    /// Interval for the infra timer that flushes delivered message IDs. Read once at grain
    /// activation to register the timer, so it stays bound via IOptions (not the config grain).
    /// </summary>
    public int DeliveredFlushIntervalMs { get; init; } = 10000;

    /// <summary>Maximum number of friends a player may have. Served live from <c>IServerConfigGrain</c>.</summary>
    public const string MaxFriendsKey = "messenger.max_friends";
    public const int MaxFriendsDefault = 300;

    /// <summary>Maximum messenger message-history rows returned. Served live from <c>IServerConfigGrain</c>.</summary>
    public const string MessageHistoryLimitKey = "messenger.message_history_limit";
    public const int MessageHistoryLimitDefault = 50;
}
