namespace Vortex.Primitives.Navigator.Enums;

public enum NavigatorQueryType
{
    AllRooms = 0,
    MyRooms = 1,
    MyFavorites = 2,
    FriendsRooms = 3,
    WithFriends = 4,
    History = 5,
    FrequentHistory = 6,
    WithRights = 7,
    RoomAds = 8,
    ByFlatCategory = 9,
    Popular = 10,
    HighestScore = 11,
    Recommended = 12,
    StaffPicks = 13,
    TextSearch = 14,
    MyGroups = 15,
    GuildBases = 16,

    /// <summary>Rooms entered in a room competition. No competition subsystem exists yet, so this
    /// resolves to an empty result set on purpose — the alternative (falling through to
    /// <see cref="AllRooms"/>) advertised the entire hotel as competition entrants.</summary>
    Competition = 17,

    /// <summary>Rooms whose active room advertisement is filed under a navigator event category --
    /// the client's <c>eventcategory__&lt;id&gt;</c> search codes, where the id is a
    /// <c>navigator_eventcats</c> row.</summary>
    EventCategory = 18,
}
