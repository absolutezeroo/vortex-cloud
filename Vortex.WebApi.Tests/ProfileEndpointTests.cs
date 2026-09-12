using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Database.Context;
using Vortex.Database.Entities.Messenger;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.FriendList.Enums;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// The public profile reads, through the real routing pipeline against a seeded database.
/// </summary>
/// <remarks>
/// Two of the four lists are covered here, and deliberately: the badge split is the only query in
/// the service with a rule of its own (worn badges are the ones with a slot, and the header carries
/// those while the card carries everything), and the friends list is the pattern every other list
/// repeats — a <c>Where</c> that has to exclude a soft-deleted row, then a projection. Rooms and
/// groups need a room model and a group's two required navigations to seed, for a query of the very
/// same shape; the cost is not worth what it would prove.
/// </remarks>
public sealed class ProfileEndpointTests
{
    [Fact]
    public async Task Lookup_AnswersTheHeaderAndOnlyTheWornBadges()
    {
        await using WebApiTestFactory factory = new();
        await SeedPlayerAsync(factory, 1, "Admin");
        await SeedBadgeAsync(factory, 1, "ADM", slot: 1);
        await SeedBadgeAsync(factory, 1, "OWNED", slot: null);

        JsonElement user = await GetJsonAsync(factory.Client, "/api/public/users?name=Admin");

        user.GetProperty("uniqueId").GetString().Should().Be("1");
        user.GetProperty("name").GetString().Should().Be("Admin");
        Codes(user.GetProperty("selectedBadges")).Should().Equal("ADM");
    }

    [Fact]
    public async Task Lookup_IsA404ForAnUnknownName()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync(
            "/api/public/users?name=Nobody"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Profile_CarriesEveryBadgeOwned_NotJustTheWornOnes()
    {
        await using WebApiTestFactory factory = new();
        await SeedPlayerAsync(factory, 1, "Admin");
        await SeedBadgeAsync(factory, 1, "ADM", slot: 1);
        await SeedBadgeAsync(factory, 1, "OWNED", slot: null);

        JsonElement profile = await GetJsonAsync(factory.Client, "/api/public/users/1/profile");

        Codes(profile.GetProperty("badges")).Should().BeEquivalentTo("ADM", "OWNED");
        Codes(profile.GetProperty("user").GetProperty("selectedBadges")).Should().Equal("ADM");
    }

    [Fact]
    public async Task Profile_HidesADeletedFriend()
    {
        // DeletedAt comes from VortexEntity and nothing filters it automatically: a deleted avatar
        // stays in the friend rows of everyone who knew it.
        await using WebApiTestFactory factory = new();
        await SeedPlayerAsync(factory, 1, "Admin");
        await SeedPlayerAsync(factory, 2, "Kaya");
        await SeedPlayerAsync(factory, 3, "Ghost", deleted: true);
        await SeedFriendAsync(factory, 1, 2);
        await SeedFriendAsync(factory, 1, 3);

        JsonElement profile = await GetJsonAsync(factory.Client, "/api/public/users/1/profile");

        JsonElement friends = profile.GetProperty("friends");
        friends.GetArrayLength().Should().Be(1);
        friends[0].GetProperty("name").GetString().Should().Be("Kaya");
    }

    [Fact]
    public async Task Profile_OfAPrivateAvatar_IsItsHeaderAndNothingElse()
    {
        // Not a 404: the player exists, and answering "no such habbo" would make this route a way to
        // test whether a name is taken. The header is what habbo.com leaves visible too.
        await using WebApiTestFactory factory = new();
        await SeedPlayerAsync(factory, 1, "Admin", visible: false);
        await SeedBadgeAsync(factory, 1, "ADM", slot: 1);
        await SeedPlayerAsync(factory, 2, "Kaya");
        await SeedFriendAsync(factory, 1, 2);

        JsonElement profile = await GetJsonAsync(factory.Client, "/api/public/users/1/profile");

        profile.GetProperty("user").GetProperty("name").GetString().Should().Be("Admin");
        profile.GetProperty("user").GetProperty("profileVisible").GetBoolean().Should().BeFalse();
        profile.GetProperty("badges").GetArrayLength().Should().Be(0);
        profile.GetProperty("friends").GetArrayLength().Should().Be(0);
        profile.GetProperty("rooms").GetArrayLength().Should().Be(0);
        profile.GetProperty("groups").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Profile_IsA404ForAnIdThatIsNotANumber()
    {
        await using WebApiTestFactory factory = new();

        HttpResponseMessage response = await factory.Client.GetAsync(
            "/api/public/users/not-an-id/profile"
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Public unless a test says otherwise. The column's own default is the opposite — a profile is
    /// private until its owner publishes it — so every test that reads a list has to opt in, and the
    /// one that does not is <see cref="Profile_OfAPrivateAvatar_IsItsHeaderAndNothingElse"/>.
    /// </summary>
    private static async Task SeedPlayerAsync(
        WebApiTestFactory factory,
        int id,
        string name,
        bool deleted = false,
        bool visible = true
    )
    {
        await using VortexDbContext db = await factory.DbContexts.CreateDbContextAsync();

        db.Players.Add(
            new PlayerEntity
            {
                Id = id,
                Name = name,
                Figure = "hr-115",
                Motto = $"{name} was here",
                Gender = AvatarGenderType.Male,
                PlayerStatus = PlayerStatusType.Offline,
                PlayerPerks = PlayerPerkFlags.None,
                ProfileVisible = visible,
                DeletedAt = deleted ? System.DateTime.UtcNow : null,
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedBadgeAsync(
        WebApiTestFactory factory,
        int playerId,
        string code,
        int? slot
    )
    {
        await using VortexDbContext db = await factory.DbContexts.CreateDbContextAsync();

        db.PlayerBadges.Add(
            new PlayerBadgeEntity
            {
                PlayerEntityId = playerId,
                BadgeCode = code,
                SlotId = slot,
                PlayerEntity = null!,
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedFriendAsync(WebApiTestFactory factory, int playerId, int friendId)
    {
        await using VortexDbContext db = await factory.DbContexts.CreateDbContextAsync();

        db.MessengerFriends.Add(
            new MessengerFriendEntity
            {
                PlayerEntityId = playerId,
                FriendPlayerEntityId = friendId,
                RelationType = MessengerFriendRelationType.Zero,
                PlayerEntity = null!,
                FriendPlayerEntity = null!,
            }
        );

        await db.SaveChangesAsync();
    }

    private static string[] Codes(JsonElement badges) =>
        badges.EnumerateArray().Select(badge => badge.GetProperty("code").GetString()!).ToArray();

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);

        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
