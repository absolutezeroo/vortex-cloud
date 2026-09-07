using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api;
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
/// It asserts on the <c>kind</c> discriminator and the presence of each section rather than on every
/// field. Pinning all of it would pin the anonymous types themselves, and a test that fails when a
/// column is renamed teaches nothing about the split.
/// </para>
/// </remarks>
public sealed class DirectorySearchTests
{
    [Fact]
    public async Task An_unrecognised_term_says_so_rather_than_guessing()
    {
        object result = await Search(NewOptions(), "not-an-id-or-a-correlation");

        Kind(result).Should().Be("unknown");
        Field(result, "hint").Should().NotBeNull("an operator needs to be told what a term may be");
    }

    [Fact]
    public async Task A_thirty_two_character_hex_term_is_read_as_a_correlation_id()
    {
        // The one branch that is chosen by the SHAPE of the term rather than by what is in the
        // database, so it answers even when nothing correlates.
        object result = await Search(NewOptions(), Guid.NewGuid().ToString("N"));

        Kind(result).Should().Be("correlationId");
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

        object result = await Search(options, correlation);

        Kind(result).Should().Be("correlationId");
        Field(result, "audit").Should().NotBeNull();
    }

    [Fact]
    public async Task A_number_is_read_as_a_player_id_and_answers_even_for_nobody()
    {
        // Deliberate: an id that matches no player still returns the profile shape with an empty
        // player, because the same answer carries the item and room history for that id.
        object result = await Search(NewOptions(), "4312");

        Kind(result).Should().Be("id");
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

        object result = await Search(options, "4312");

        Kind(result).Should().Be("id");
        Field(result, "playerProfile").Should().NotBeNull();
    }

    [Fact]
    public async Task The_profile_endpoint_answers_the_profile_itself()
    {
        // Not the search envelope: no "kind", no sections, just what the popup renders. The whole
        // point of the split is that this caller stops paying for the investigation's queries.
        DbContextOptions<VortexDbContext> options = NewOptions();

        await using (VortexDbContext db = new(options))
        {
            db.Players.Add(NewPlayer(4312));
            await db.SaveChangesAsync();
        }

        object? profile = await Api(options)
            .PlayerProfileAsync(4312, new NameValueCollection(), CancellationToken.None);

        profile.Should().NotBeNull();
        Field(profile!, "Name").Should().Be("Someone");
        Kind(profile!).Should().BeNull("the profile is the answer, not a branch of one");
    }

    [Fact]
    public async Task The_profile_endpoint_answers_null_for_an_id_that_is_nobody()
    {
        // The popup shows its "not found" state on null, so this is the contract it relies on.
        object? profile = await Api(NewOptions())
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
    /// Seven of its ten dependencies are null on purpose: the search reads the database, asks
    /// <see cref="DashboardAssetUrls"/> for an avatar and a furniture icon, and asks the session
    /// gateway who is online. Passing fakes for the rest would suggest they take part.
    /// </remarks>
    private static Task<object> Search(DbContextOptions<VortexDbContext> options, string term) =>
        Api(options).SearchAsync(new NameValueCollection { ["q"] = term }, CancellationToken.None);

    private static DashboardApiService Api(DbContextOptions<VortexDbContext> options) =>
        new(
            new TestContextFactory(options),
            null!,
            // The profile answers "is this player connected right now", which is the one thing the
            // search asks outside the database. Nobody is connected in a test.
            FakeProxy.Create<ISessionGateway>(call =>
                call.Method.Name switch
                {
                    nameof(ISessionGateway.IsOnline) => false,
                    nameof(ISessionGateway.GetOnlinePlayerCount) => 0,
                    _ => Array.Empty<PlayerId>(),
                }
            ),
            new DashboardAssetUrls(Options.Create(new ObservabilityConfig())),
            null!,
            null!,
            null!,
            null!,
            null!,
            Options.Create(new ObservabilityConfig())
        );

    /// <summary>The answers are anonymous types, so their fields are read by reflection.</summary>
    private static object? Field(object result, string name) =>
        result
            .GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(result);

    private static string? Kind(object result) => Field(result, "kind") as string;
}
