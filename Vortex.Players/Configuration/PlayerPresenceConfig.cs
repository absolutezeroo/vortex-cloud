namespace Vortex.Players.Configuration;

public sealed class PlayerPresenceConfig
{
    public const string SECTION_NAME = "Turbo:PlayerPresence";

    public int MaxOutgoingQueueSize { get; init; } = 500;
    public int FurniInventoryFragmentSize { get; init; } = 100;
}
