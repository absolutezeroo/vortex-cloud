using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api.Catalogue.Contracts;
using Vortex.Dashboard.API.Api.Hotel.Contracts;
using Vortex.Dashboard.API.Api.Platform.Contracts;
using Vortex.Dashboard.API.Api.Progression.Contracts;
using Vortex.Dashboard.API.Api.Safety.Contracts;
using Vortex.Dashboard.API.Hosting;
using Vortex.Observability.Configuration;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Which routes tell OpenAPI what they answer with.
/// </summary>
/// <remarks>
/// <para>
/// Every dashboard handler returns <see cref="IResult" />, which carries no type. So a route's
/// response schema exists only if the mapping declared one, and a document without schemas
/// generates front-end types that are all <c>unknown</c> — the whole point of §11 lost with nothing
/// failing anywhere. This test is what makes "the API contract is the front end's source of truth"
/// a checkable claim rather than an intention.
/// </para>
/// <para>
/// It pins the subjects that have been converted, not a total. Declaring a response type is only
/// possible once a subject's reads return real types instead of <c>object</c>, so this list grows
/// one subject at a time and each addition is a deliberate line here.
/// </para>
/// </remarks>
public sealed class DashboardResponseSchemaTests
{
    /// <summary>Route to the type it must publish. One entry per converted read.</summary>
    private static readonly (string Route, Type Response)[] Declared =
    [
        ("/api/v1/polls", typeof(PollListResponse)),
        ("/api/v1/polls/question-types", typeof(PollQuestionTypeOptions)),
        ("/api/v1/polls/{pollId:int}", typeof(PollDetail)),
        ("/api/v1/polls/{pollId:int}/results", typeof(PollResults)),
        ("/api/v1/articles", typeof(ArticleListResponse)),
        ("/api/v1/articles/meta", typeof(ArticleFormMeta)),
        ("/api/v1/articles/images", typeof(ArticleImageBrowse)),
        ("/api/v1/articles/{articleId:int}", typeof(ArticleDetail)),
        ("/api/v1/wired/stats", typeof(WiredStats)),
        ("/api/v1/pets/stats", typeof(PetStats)),
        ("/api/v1/cfh/stats", typeof(CfhStats)),
        ("/api/v1/chatlogs", typeof(ChatlogPage)),
        ("/api/v1/songs", typeof(SongListResponse)),
        ("/api/v1/groups/stats", typeof(GroupStats)),
        ("/api/v1/social/stats", typeof(SocialStats)),
        ("/api/v1/bots", typeof(BotListResponse)),
        ("/api/v1/bots/stats", typeof(BotStats)),
        ("/api/v1/bots/{botId:int}", typeof(BotDetail)),
        ("/api/v1/hand-items", typeof(HandItemList)),
        ("/api/v1/forensics/audit", typeof(AuditPage)),
        ("/api/v1/forensics/moderation/stats", typeof(ModerationStats)),
        ("/api/v1/catalog/purchases/stats", typeof(CatalogPurchaseStats)),
        ("/api/v1/economy/trends", typeof(EconomyTrends)),
        ("/api/v1/economy/ledger", typeof(EconomyLedgerPage)),
        ("/api/v1/economy/marketplace", typeof(MarketplaceSummary)),
        ("/api/v1/economy/subscriptions", typeof(ClubSubscriptions)),
        ("/api/v1/economy/extras", typeof(EconomyExtras)),
        ("/api/v1/rentable-spaces/activity", typeof(RentableSpaceAuditPage)),
        ("/api/v1/directory/groups", typeof(DirectoryPage)),
        ("/api/v1/directory/badges", typeof(CodeDirectoryPage)),
        ("/api/v1/directory/quest-campaigns", typeof(CodeDirectoryPage)),
        ("/api/v1/directory/players", typeof(PlayerDirectoryPage)),
        ("/api/v1/directory/rooms", typeof(RoomDirectoryPage)),
        ("/api/v1/directory/furniture", typeof(FurnitureDirectoryPage)),
        ("/api/v1/directory/avatars", typeof(AvatarBatch)),
    ];

    [Fact]
    public void EveryConvertedReadPublishesItsResponseSchema()
    {
        IReadOnlyDictionary<string, Type[]> published = PublishedResponseTypes();

        foreach ((string route, Type response) in Declared)
        {
            published
                .Should()
                .ContainKey(route, "the route must still exist for its schema to mean anything");

            published[route]
                .Should()
                .Contain(
                    response,
                    "{0} answers with {1}, and a generator can only know that from the document",
                    route,
                    response.Name
                );
        }
    }

    [Fact]
    public void ALookupThatCanAnswerNothingSaysSoInTheDocument()
    {
        // Both poll lookups return 404 for an id that is nobody. A generated client that does not
        // know this treats the 404 body as the poll itself.
        IReadOnlyDictionary<string, int[]> statuses = PublishedStatusCodes();

        statuses["/api/v1/polls/{pollId:int}"].Should().Contain(StatusCodes.Status404NotFound);
        statuses["/api/v1/polls/{pollId:int}/results"]
            .Should()
            .Contain(StatusCodes.Status404NotFound);
        statuses["/api/v1/articles/{articleId:int}"]
            .Should()
            .Contain(StatusCodes.Status404NotFound);
    }

    private static IReadOnlyDictionary<string, Type[]> PublishedResponseTypes() =>
        Endpoints()
            .ToDictionary(
                endpoint => Pattern(endpoint),
                endpoint =>
                    endpoint
                        .Metadata.OfType<IProducesResponseTypeMetadata>()
                        .Select(metadata => metadata.Type)
                        .Where(type => type is not null)
                        .Select(type => type!)
                        .ToArray(),
                StringComparer.Ordinal
            );

    private static IReadOnlyDictionary<string, int[]> PublishedStatusCodes() =>
        Endpoints()
            .ToDictionary(
                endpoint => Pattern(endpoint),
                endpoint =>
                    endpoint
                        .Metadata.OfType<IProducesResponseTypeMetadata>()
                        .Select(metadata => metadata.StatusCode)
                        .ToArray(),
                StringComparer.Ordinal
            );

    private static string Pattern(Endpoint endpoint) =>
        "/" + ((RouteEndpoint)endpoint).RoutePattern.RawText!.TrimStart('/');

    private static List<Endpoint> Endpoints()
    {
        WebApplication app = BuildApp();

        DashboardEndpoints.MapReadApi(app, () => DateTime.UnixEpoch);

        return
        [
            .. ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>(),
        ];
    }

    /// <summary>
    /// The same container the host builds, with factories that throw: route building never resolves
    /// a service, it only asks whether the container knows the type.
    /// </summary>
    private static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();

        foreach (Type serviceType in DashboardWebHost.ForwardedServiceTypes)
        {
            builder.Services.AddSingleton(
                serviceType,
                _ =>
                    throw new InvalidOperationException(
                        $"{serviceType.Name} was resolved; this test only builds routes."
                    )
            );
        }

        builder.Services.AddSingleton<IOptions<ObservabilityConfig>>(
            Options.Create(new ObservabilityConfig())
        );

        return builder.Build();
    }
}
