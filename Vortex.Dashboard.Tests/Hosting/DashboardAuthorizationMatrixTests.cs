using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Hosting;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The dashboard's first security property is that each route demands the right capability, and it
/// rests on ~200 constants typed by hand into <c>MapReadGet</c>/<c>MapPost</c> calls. Nothing
/// enforces the pairing: a copy-paste that leaves <c>EconomyRead</c> on a staff route compiles,
/// runs, and quietly hands the staff roster to whoever holds economy access. Neither the build nor
/// the quality gate can see it, and neither can a reviewer skimming a 40-line diff of near-identical
/// Map calls.
///
/// <para>
/// So the matrix is written down. <c>authorization-matrix.txt</c> is generated, never hand-edited:
/// when this test fails it drops the current matrix beside the expected one and names both paths,
/// and the fix is to copy it over and read the diff. Adding an endpoint is one new line -- adding
/// one that answers to the wrong capability is a line that moves, which is the point.
/// </para>
///
/// <para>
/// Not covered here: the mandatory audited reason. Endpoint filters are compiled into the request
/// delegate rather than published as metadata, so a write route mapped without going through
/// <c>MapPost</c> -- and therefore without <c>DashboardRequestValidationFilter</c> -- stays
/// invisible from the outside. That one needs a real request to catch.
/// </para>
/// </summary>
public sealed class DashboardAuthorizationMatrixTests
{
    private const string MATRIX_FILE = "authorization-matrix.txt";

    /// <summary>
    /// The only routes allowed to answer without a session. Both are deliberate, both audit
    /// themselves, and login is rate-limited; anything else appearing here is a hole.
    /// </summary>
    private static readonly string[] ANONYMOUS_ROUTES = ["/api/login", "/api/logout"];

    [Fact]
    public void TheRouteToCapabilityMatrixIsWhatTheSnapshotSays()
    {
        string actual = string.Join(Environment.NewLine, DescribeRoutes());
        string expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, MATRIX_FILE));

        if (Normalize(actual) == Normalize(expected))
        {
            return;
        }

        string actualPath = Path.Combine(
            AppContext.BaseDirectory,
            "authorization-matrix.actual.txt"
        );

        File.WriteAllText(actualPath, actual + Environment.NewLine);

        Assert.Fail(
            "The dashboard authorization matrix changed. Review the difference, then copy the "
                + $"generated file over the checked-in one:{Environment.NewLine}"
                + $"  generated: {actualPath}{Environment.NewLine}"
                + $"  expected:  Vortex.Dashboard.Tests/Hosting/{MATRIX_FILE}"
        );
    }

    /// <summary>
    /// A policy name that is not a declared capability does not make a route stricter -- it makes it
    /// unreachable, because the policies are generated from <see cref="Capabilities.Dashboard.All" />
    /// and a name that is not in there has no policy behind it at all.
    /// </summary>
    [Fact]
    public void EveryCapabilityDemandedByARouteIsADeclaredOne()
    {
        string[] declared = [.. Capabilities.Dashboard.All];

        foreach (RouteDescriptor route in EnumerateRoutes())
        {
            foreach (string capability in route.Capabilities)
            {
                declared
                    .Should()
                    .Contain(
                        capability,
                        "{0} {1} demands it, and Capabilities.Dashboard.All is what the policies are built from",
                        route.Methods,
                        route.Route
                    );
            }
        }
    }

    /// <summary>
    /// Every route lived at two paths until the unversioned ones were removed: <c>/api/v1/x</c> and
    /// <c>/api/x</c>, mapped from the same helper, doubling the surface for a prefix nothing read.
    /// The three below never had a twin. A route reappearing outside <c>/api/v1/</c> means someone
    /// mapped one by hand instead of through <c>MapReadGet</c>/<c>MapPost</c>, which also means it
    /// skipped whatever those two attach.
    /// </summary>
    [Fact]
    public void EveryRouteIsVersioned()
    {
        string[] unversioned =
        [
            .. EnumerateRoutes()
                .Select(route => route.Route)
                .Where(route => !route.StartsWith("/api/v1/", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];

        unversioned.Should().BeEquivalentTo(["/api/login", "/api/logout", "/api/me"]);
    }

    [Fact]
    public void OnlyLoginAndLogoutAnswerWithoutASession()
    {
        string[] anonymous =
        [
            .. EnumerateRoutes()
                .Where(route => route.AllowsAnonymous)
                .Select(route => route.Route)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];

        anonymous.Should().BeEquivalentTo(ANONYMOUS_ROUTES);
    }

    /// <summary>
    /// Every remaining route carries authorization metadata of some kind -- a named capability, or
    /// (for <c>/api/me</c>, which only reports who the caller already is) the bare authenticated
    /// check. A route with neither would be world-readable.
    /// </summary>
    [Fact]
    public void NoRouteIsLeftUnguarded()
    {
        string[] unguarded =
        [
            .. EnumerateRoutes()
                .Where(route => !route.AllowsAnonymous && !route.RequiresAuthorization)
                .Select(route => $"{route.Methods} {route.Route}")
                .Order(StringComparer.Ordinal),
        ];

        unguarded.Should().BeEmpty();
    }

    private static string Normalize(string value) => value.ReplaceLineEndings("\n").TrimEnd();

    private static IEnumerable<string> DescribeRoutes() =>
        EnumerateRoutes().Select(Describe).Order(StringComparer.Ordinal);

    private static string Describe(RouteDescriptor route)
    {
        string demand =
            route.AllowsAnonymous ? "ANONYMOUS"
            : route.Capabilities.Length > 0 ? string.Join(",", route.Capabilities)
            : "AUTHENTICATED";

        return $"{route.Methods} {route.Route} -> {demand}";
    }

    private static List<RouteDescriptor> EnumerateRoutes()
    {
        WebApplication app = BuildApp();

        DashboardEndpoints.MapAuth(app);
        DashboardEndpoints.MapReadApi(app, () => DateTime.UnixEpoch);
        DashboardEndpoints.MapOperations(app);
        DashboardEndpoints.MapMeta(app);
        DashboardEndpoints.MapAccountEndpoints(app);

        return
        [
            .. ((IEndpointRouteBuilder)app)
                .DataSources.SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(Read)
                .Where(route => route.Route.StartsWith("/api/", StringComparison.Ordinal)),
        ];
    }

    private static RouteDescriptor Read(RouteEndpoint endpoint)
    {
        IAuthorizeData[] authorize = [.. endpoint.Metadata.OfType<IAuthorizeData>()];

        return new RouteDescriptor(
            string.Join(
                "|",
                endpoint
                    .Metadata.OfType<IHttpMethodMetadata>()
                    .SelectMany(metadata => metadata.HttpMethods)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ),
            endpoint.RoutePattern.RawText ?? "",
            [
                .. authorize
                    .Select(data => data.Policy)
                    .OfType<string>()
                    .Where(policy => !string.IsNullOrWhiteSpace(policy))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal),
            ],
            endpoint.Metadata.OfType<IAllowAnonymous>().Any(),
            authorize.Length > 0
        );
    }

    /// <summary>
    /// Mirrors <c>DashboardWebHost.ForwardSingletons</c> the same way
    /// <see cref="DashboardEndpointServiceTests" /> does: the forwarded types resolve to a throw, so
    /// route building sees the container the host gives it without a silo or a database behind it.
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
                        $"{serviceType.Name} was resolved; this test only exercises route building."
                    )
            );
        }

        builder.Services.AddSingleton<IOptions<ObservabilityConfig>>(
            Options.Create(new ObservabilityConfig())
        );

        return builder.Build();
    }

    private readonly record struct RouteDescriptor(
        string Methods,
        string Route,
        string[] Capabilities,
        bool AllowsAnonymous,
        bool RequiresAuthorization
    );
}
