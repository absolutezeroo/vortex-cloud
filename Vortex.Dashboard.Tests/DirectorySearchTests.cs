using System;
using System.Collections.Specialized;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Api.Platform;
using Vortex.Dashboard.API.Api.Platform.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Audit;
using Vortex.Database.Entities.Players;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Dashboard.Tests;

/// <summary>
/// What the investigation search answers, branch by branch.
/// </summary>
/// <remarks>
/// <para>
/// Written before splitting <c>SearchAsync</c>, which was 716 lines in one method and had no test at
/// all. It is a characterisation test: it does not argue that these answers are right, it pins what
/// they are, so the split can be shown to change nothing. That is the only honest way to take a
/// method of that size apart.
/// </para>
/// <para>
/// Each branch is now its own record, so a branch is identified by the type it returns rather than
/// by reading a <c>kind</c> field off an anonymous object. The tag itself has moved to the
/// serializer, which is why one test below reads the JSON: the front end switches on <c>kind</c>,
/// and nothing else would notice if it stopped being written.
/// </para>
/// </remarks>
public sealed class DirectorySearchTests
{
    [Fact]
    public async Task An_unrecognised_term_says_so_rather_than_guessing()
    {
        DirectorySearch result = await SearchAsync(NewOptions(), "not-an-id-or-a-correlation");

        result
            .Should()
            .BeOfType<UnknownSearch>()
            .Which.Hint.Should()
            .NotBeNullOrWhiteSpace("an operator needs to be told what a term may be");
    }

    [Fact]
    public async Task The_kind_the_page_switches_on_is_written_onto_the_wire()
    {
        // No record declares "kind" any more -- the serializer writes it from the attributes on
        // DirectorySearch. That is invisible to every other test here, and the investigation page
        // is a switch over exactly this string.
        DirectorySearch result = await SearchAsync(NewOptions(), "not-an-id-or-a-correlation");

        string json = JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

        json.Should().StartWith("{\"kind\":\"unknown\"");
    }

    [Fact]
    public async Task A_thirty_two_character_hex_term_is_read_as_a_correlation_id()
    {
        // The one branch that is chosen by the SHAPE of the term rather than by what is in the
        // database, so it answers even when nothing correlates.
        DirectorySearch result = await SearchAsync(NewOptions(), Guid.NewGuid().ToString("N"));

        result.Should().BeOfType<CorrelationSearch>();
    }

    [Fact]
    public async Task A_correlation_id_gathers_the_audit_trail_it_names()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        string correlation = Guid.NewGuid().ToString("N");

        await using (VortexDbContext db = new(options))
        {
            db.AuditEvents.Add(
                new AuditEventEntity
                {
                    CorrelationId = correlation,
                    Category = AuditCategory.Moderation,
                    Action = "did.something",
                    Severity = AuditSeverity.Info,
                    Result = AuditResult.Success,
                    OccurredAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();
        }

        DirectorySearch result = await SearchAsync(options, correlation);

        result
            .Should()
            .BeOfType<CorrelationSearch>()
            .Which.Audit.Should()
            .ContainSingle()
            .Which.Action.Should()
            .Be("did.something");
    }

    [Fact]
    public async Task A_number_is_read_as_a_player_id_and_answers_even_for_nobody()
    {
        // Deliberate: an id that matches no player still returns the id shape with a null profile,
        // because the same answer carries the item and room history for that id.
        DirectorySearch result = await SearchAsync(NewOptions(), "4312");

        result.Should().BeOfType<IdSearch>().Which.PlayerProfile.Should().BeNull();
    }

    [Fact]
    public async Task A_players_id_brings_back_their_profile()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.Players.Add(NewPlayer(4312));
            await db.SaveChangesAsync();
        }

        DirectorySearch result = await SearchAsync(options, "4312");

        result.Should().BeOfType<IdSearch>().Which.PlayerProfile!.Name.Should().Be("Someone");
    }

    [Fact]
    public async Task The_profile_endpoint_answers_the_profile_itself()
    {
        // Not the search envelope: no kind, no sections, just what the popup renders. The whole
        // point of the split is that this caller stops paying for the investigation's queries.
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.Players.Add(NewPlayer(4312));
            await db.SaveChangesAsync();
        }

        PlayerProfile? profile = await Reads(options)
            .PlayerProfileAsync(4312, new NameValueCollection(), CancellationToken.None);

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Someone");
        profile.Online.Should().BeFalse("nobody is connected in a test");
    }

    [Fact]
    public async Task The_profile_endpoint_answers_null_for_an_id_that_is_nobody()
    {
        // The popup shows its "not found" state on null, so this is the contract it relies on.
        PlayerProfile? profile = await Reads(NewOptions())
            .PlayerProfileAsync(4312, new NameValueCollection(), CancellationToken.None);

        profile.Should().BeNull();
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private static PlayerEntity NewPlayer(int id) =>
        new()
        {
            Id = id,
            Name = "Someone",
            Motto = "hello",
            Figure = "hd-180-1",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };

    private sealed class TestContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"directory-search-{Guid.NewGuid():N}")
            .Options;

    /// <summary>
    /// Builds the service with only what this path uses.
    /// </summary>
    /// <remarks>
    /// Every dependency is real now: the search reads the database, asks
    /// <see cref="DashboardAssetUrls"/> for an avatar and a furniture icon, and asks the session
    /// gateway who is online. When this lived on a class with ten dependencies, six had to be null
    /// with a comment explaining that they took no part.
    /// </remarks>
    private static Task<DirectorySearch> SearchAsync(
        DbContextOptions<VortexDbContext> options,
        string term
    ) =>
        Reads(options)
            .SearchAsync(new NameValueCollection { ["q"] = term }, CancellationToken.None);

    private static DirectoryReads Reads(DbContextOptions<VortexDbContext> options) =>
        new(
            new TestContextFactory(options),
            new DashboardAssetUrls(Options.Create(new ObservabilityConfig())),
            // The profile answers "is this player connected right now", which is the one thing the
            // search asks outside the database. Nobody is connected in a test.
            FakeProxy.Create<ISessionGateway>(call =>
                call.Method.Name switch
                {
                    nameof(ISessionGateway.IsOnline) => false,
                    nameof(ISessionGateway.GetOnlinePlayerCount) => 0,
                    _ => Array.Empty<PlayerId>(),
                }
            )
        );
}
