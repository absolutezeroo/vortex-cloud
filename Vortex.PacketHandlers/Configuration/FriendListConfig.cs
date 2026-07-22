namespace Vortex.PacketHandlers.Configuration;

/// <summary>
/// Config keys and defaults for friend-list limits, served live from <c>IServerConfigGrain</c>
/// (migrated off IOptions/appsettings). The default is the fallback when a key has no admin override
/// stored in the DB.
/// </summary>
public static class FriendListConfig
{
    public const string FragmentSizeKey = "friendlist.fragment_size";
    public const int FragmentSizeDefault = 500;

    public const string UserFriendLimitKey = "friendlist.user_limit";
    public const int UserFriendLimitDefault = 300;

    public const string NormalFriendLimitKey = "friendlist.normal_limit";
    public const int NormalFriendLimitDefault = 300;

    public const string ExtendedFriendLimitKey = "friendlist.extended_limit";
    public const int ExtendedFriendLimitDefault = 2000;

    public const string SearchLimitKey = "friendlist.search_limit";
    public const int SearchLimitDefault = 30;
}
