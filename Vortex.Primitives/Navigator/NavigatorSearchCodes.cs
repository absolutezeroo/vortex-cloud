using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Vortex.Primitives.Navigator.Enums;

namespace Vortex.Primitives.Navigator;

/// <summary>
/// The search codes the client sends with every navigator request, and the query each one means.
/// </summary>
/// <remarks>
/// <para>
/// The codes are hardcoded client-side (one per navigator tab/button), so the server must know them
/// without help from the database. <c>navigator_top_level_contexts</c>/<c>navigator_quick_links</c>
/// stay authoritative — an operator can repoint a code at a different query — but a missing or
/// unseeded row must not silently turn a search into "every room in the hotel", which is exactly
/// what happened before: only <c>groups</c> and <c>my_groups</c> had a built-in fallback, so an
/// unconfigured hotel answered "my rooms" and "my favourites" with the whole room table.
/// </para>
/// <para>
/// The spellings are not guesses: they are the keys the client localizes a result block's header
/// with (<c>navigator.searchcode.title.&lt;code&gt;</c> in <c>external_texts.json</c>, applied in
/// <c>BlockResultsView</c>/<c>QuickLinksView</c>). A block sent under a code that has no such key
/// renders with the raw code as its title, which is how the favourites tab came to be labelled
/// "myf".
/// </para>
/// </remarks>
public static class NavigatorSearchCodes
{
    public const string HotelView = "hotel_view";
    public const string MyWorldView = "myworld_view";
    public const string OfficialView = "official_view";
    public const string RoomAdsView = "roomads_view";

    public const string Categories = "categories";
    public const string Competition = "competition";
    public const string Favourites = "favorites";
    public const string FriendsRooms = "friends_rooms";
    public const string GuildBases = "groups";
    public const string HighestScore = "highest_score";
    public const string History = "history";
    public const string HistoryFrequent = "history_freq";
    public const string MyGuildBases = "my_groups";
    public const string MyRooms = "my";
    public const string Official = "official";
    public const string Popular = "popular";
    public const string Recommended = "recommended";
    public const string RoomAds = "new_ads";
    public const string StaffPicks = "staffpicks";
    public const string TextSearch = "query";
    public const string TopPromotions = "top_promotions";
    public const string WithFriends = "with_friends";
    public const string WithRights = "with_rights";

    public const string EventCategoryPrefix = "eventcategory__";
    public const string FlatCategoryPrefix = "category__";

    public static string FlatCategoryCode(int categoryId) =>
        FlatCategoryPrefix + categoryId.ToString(CultureInfo.InvariantCulture);

    public static readonly ImmutableHashSet<string> TopLevelViews = ImmutableHashSet.Create(
        HotelView,
        MyWorldView,
        OfficialView,
        RoomAdsView
    );

    public static readonly ImmutableDictionary<string, NavigatorQueryType> QueryTypeBySearchCode =
        ImmutableDictionary.CreateRange([
            KeyValuePair.Create(Categories, NavigatorQueryType.ByFlatCategory),
            KeyValuePair.Create(Competition, NavigatorQueryType.Competition),
            KeyValuePair.Create(Favourites, NavigatorQueryType.MyFavorites),
            KeyValuePair.Create(FriendsRooms, NavigatorQueryType.FriendsRooms),
            KeyValuePair.Create(GuildBases, NavigatorQueryType.GuildBases),
            KeyValuePair.Create(HighestScore, NavigatorQueryType.HighestScore),
            KeyValuePair.Create(History, NavigatorQueryType.History),
            KeyValuePair.Create(HistoryFrequent, NavigatorQueryType.FrequentHistory),
            KeyValuePair.Create(HotelView, NavigatorQueryType.Popular),
            KeyValuePair.Create(MyGuildBases, NavigatorQueryType.MyGroups),
            KeyValuePair.Create(MyRooms, NavigatorQueryType.MyRooms),
            KeyValuePair.Create(MyWorldView, NavigatorQueryType.MyRooms),
            KeyValuePair.Create(Official, NavigatorQueryType.StaffPicks),
            KeyValuePair.Create(OfficialView, NavigatorQueryType.StaffPicks),
            KeyValuePair.Create(Popular, NavigatorQueryType.Popular),
            KeyValuePair.Create(RoomAdsView, NavigatorQueryType.RoomAds),
            KeyValuePair.Create(Recommended, NavigatorQueryType.Recommended),
            KeyValuePair.Create(RoomAds, NavigatorQueryType.RoomAds),
            KeyValuePair.Create(StaffPicks, NavigatorQueryType.StaffPicks),
            KeyValuePair.Create(TextSearch, NavigatorQueryType.TextSearch),
            KeyValuePair.Create(TopPromotions, NavigatorQueryType.RoomAds),
            KeyValuePair.Create(WithFriends, NavigatorQueryType.WithFriends),
            KeyValuePair.Create(WithRights, NavigatorQueryType.WithRights),
        ]);
}
