namespace Vortex.Players.Configuration;

public sealed class MessengerConfig
{
    public const string SECTION_NAME = "Turbo:Messenger";

    public int MaxFriends { get; init; } = 300;
    public int MessageHistoryLimit { get; init; } = 50;
    public int DeliveredFlushIntervalMs { get; init; } = 10000;
}
