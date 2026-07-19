namespace Vortex.PacketHandlers.Configuration;

public sealed class FriendListConfig
{
    public const string SECTION_NAME = "Vortex:FriendList";

    public int FragmentSize { get; init; } = 500;
    public int UserFriendLimit { get; init; } = 300;
    public int NormalFriendLimit { get; init; } = 300;
    public int ExtendedFriendLimit { get; init; } = 2000;
    public int SearchLimit { get; init; } = 30;
}
