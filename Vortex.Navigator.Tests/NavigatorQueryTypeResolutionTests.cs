using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Database.Context;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Navigator.Tests;

/// <summary>
/// Every navigator tab sends a search code the client hardcodes. Nothing seeded
/// <c>navigator_top_level_contexts</c>, so on a fresh hotel the lookup found no row for any of them
/// and only <c>groups</c>/<c>my_groups</c> had a built-in fallback — every other tab resolved to
/// <see cref="NavigatorQueryType.AllRooms"/> and answered with the whole room table.
/// </summary>
public sealed class NavigatorQueryTypeResolutionTests
{
    // ResolveQueryType reads cached reference data, never the database, so the factory is never
    // touched -- but the constructor still wants one.
    private static readonly NavigatorProvider Provider = new(
        FakeProxy.Create<IDbContextFactory<VortexDbContext>>(_ => null),
        NullLogger<NavigatorProvider>.Instance
    );

    [Theory]
    [InlineData(NavigatorSearchCodes.MyRooms, NavigatorQueryType.MyRooms)]
    [InlineData(NavigatorSearchCodes.Favourites, NavigatorQueryType.MyFavorites)]
    [InlineData(NavigatorSearchCodes.FriendsRooms, NavigatorQueryType.FriendsRooms)]
    [InlineData(NavigatorSearchCodes.WithFriends, NavigatorQueryType.WithFriends)]
    [InlineData(NavigatorSearchCodes.History, NavigatorQueryType.History)]
    [InlineData(NavigatorSearchCodes.HistoryFrequent, NavigatorQueryType.FrequentHistory)]
    [InlineData(NavigatorSearchCodes.WithRights, NavigatorQueryType.WithRights)]
    [InlineData(NavigatorSearchCodes.RoomAds, NavigatorQueryType.RoomAds)]
    [InlineData(NavigatorSearchCodes.TopPromotions, NavigatorQueryType.RoomAds)]
    [InlineData(NavigatorSearchCodes.Categories, NavigatorQueryType.ByFlatCategory)]
    [InlineData(NavigatorSearchCodes.Popular, NavigatorQueryType.Popular)]
    [InlineData(NavigatorSearchCodes.HighestScore, NavigatorQueryType.HighestScore)]
    [InlineData(NavigatorSearchCodes.Recommended, NavigatorQueryType.Recommended)]
    [InlineData(NavigatorSearchCodes.StaffPicks, NavigatorQueryType.StaffPicks)]
    [InlineData(NavigatorSearchCodes.Official, NavigatorQueryType.StaffPicks)]
    [InlineData(NavigatorSearchCodes.OfficialRoot, NavigatorQueryType.StaffPicks)]
    [InlineData(NavigatorSearchCodes.TextSearch, NavigatorQueryType.TextSearch)]
    [InlineData(NavigatorSearchCodes.GuildBases, NavigatorQueryType.GuildBases)]
    [InlineData(NavigatorSearchCodes.MyGuildBases, NavigatorQueryType.MyGroups)]
    [InlineData(NavigatorSearchCodes.Competition, NavigatorQueryType.Competition)]
    [InlineData(NavigatorSearchCodes.HotelView, NavigatorQueryType.Popular)]
    [InlineData(NavigatorSearchCodes.MyWorldView, NavigatorQueryType.MyRooms)]
    public void KnownSearchCode_ResolvesWithoutAnyDatabaseConfiguration(
        string searchCode,
        NavigatorQueryType expected
    ) => Provider.ResolveQueryType(searchCode).Should().Be(expected);

    [Theory]
    [InlineData("eventcategory__1")]
    [InlineData("eventcategory__11")]
    public void EventCategoryCode_ResolvesByItsPrefix(string searchCode) =>
        Provider.ResolveQueryType(searchCode).Should().Be(NavigatorQueryType.EventCategory);

    /// <summary>The catch-all is still there for codes nobody has taught the server about; it just
    /// no longer catches the ones the client sends on every tab.</summary>
    [Fact]
    public void UnknownSearchCode_FallsBackToAllRooms() =>
        Provider
            .ResolveQueryType("something_nobody_implemented")
            .Should()
            .Be(NavigatorQueryType.AllRooms);
}
