using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Hosting;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The one hole <see cref="DashboardAuthorizationMatrixTests" /> names and cannot close. Every
/// dashboard write is supposed to carry a reason an operator can be held to, and that rule is
/// applied by an endpoint filter attached in the <c>MapPost</c> helper. Endpoint filters are compiled
/// into the request delegate rather than published as metadata, so a write route mapped by hand --
/// skipping the helper, as the login route legitimately does -- loses the rule silently. Nothing
/// reading route metadata can tell the difference.
///
/// <para>
/// So this one sends real requests. Every operation route is posted an empty body and has to answer
/// 400 <c>invalid_request</c>: if a route ever accepts that, it is a route that would have accepted
/// an unjustified write and audited it with an empty string.
/// </para>
///
/// <para>
/// Authorization is deliberately wide open here -- every policy passes -- because who may call these
/// routes is the matrix test's subject, and mixing the two would mean a failure that could mean
/// either.
/// </para>
/// </summary>
public sealed class DashboardOperationReasonTests
{
    /// <summary>Authenticates every request, so only the reason filter can refuse one.</summary>
    private sealed class AlwaysAuthenticated(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            ClaimsPrincipal principal = new(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-operator")], "Test")
            );

            return Task.FromResult(
                AuthenticateResult.Success(new AuthenticationTicket(principal, "Test"))
            );
        }
    }

    private static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        // Minimal APIs resolve a handler's service parameters *before* the filter pipeline runs --
        // the filter inspects those resolved arguments, so it cannot run in front of them. A factory
        // that throws would therefore fire on every request and prove nothing about the filter. The
        // instances here are never touched: the filter rejects before the handler body, and none of
        // these types is read by the filter itself, which only looks for IReasonedRequest.
        foreach (Type serviceType in DashboardWebHost.ForwardedServiceTypes)
        {
            if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                continue;
            }

            builder.Services.AddSingleton(
                serviceType,
                _ => RuntimeHelpers.GetUninitializedObject(serviceType)
            );
        }

        builder.Services.AddSingleton<IOptions<ObservabilityConfig>>(
            Options.Create(new ObservabilityConfig())
        );

        builder
            .Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, AlwaysAuthenticated>("Test", null);

        builder.Services.AddAuthorization(authorization =>
        {
            foreach (string capability in Capabilities.Dashboard.All)
            {
                authorization.AddPolicy(capability, policy => policy.RequireAssertion(_ => true));
            }
        });

        WebApplication app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        DashboardEndpoints.MapOperations(app);

        return app;
    }

    private static IEnumerable<string> OperationRoutes(WebApplication app) =>
        ((IEndpointRouteBuilder)app)
            .DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
                endpoint
                    .Metadata.OfType<IHttpMethodMetadata>()
                    .Any(m => m.HttpMethods.Contains("POST"))
            )
            .Select(endpoint => endpoint.RoutePattern.RawText ?? "")
            .Where(route => route.StartsWith("/api/", StringComparison.Ordinal))
            // A route with a parameter needs a value to be reachable at all, and the reason filter
            // runs after routing either way -- the parameterless ones are the whole rule.
            .Where(route => !route.Contains('{', StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    [Fact]
    public async Task EveryOperationRefusesABodyWithNoReason()
    {
        await using WebApplication app = BuildApp();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        List<string> accepted = [];
        int checkedRoutes = 0;

        foreach (string route in OperationRoutes(app))
        {
            checkedRoutes++;

            // An empty reason rather than an absent one: the field is present and unusable, which
            // is the case an endpoint could plausibly wave through.
            HttpResponseMessage response = await client.PostAsJsonAsync(route, new { reason = "" });

            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                accepted.Add($"{route} -> {(int)response.StatusCode}");
                continue;
            }

            string payload = await response.Content.ReadAsStringAsync();

            // A 400 with no body is the framework refusing to bind the request at all, which is a
            // rejection too -- just not this filter's. Only a body that says something is checked.
            if (payload.Length == 0)
            {
                continue;
            }

            JsonDocument body = JsonDocument.Parse(payload);

            if (
                !body.RootElement.TryGetProperty("error", out JsonElement error)
                || error.GetString() != "invalid_request"
            )
            {
                accepted.Add($"{route} -> 400 but not invalid_request");
            }
        }

        checkedRoutes.Should().BeGreaterThan(20, "the operation surface is not that small");
        accepted
            .Should()
            .BeEmpty(
                "a write that answers anything else here is a write that would accept an "
                    + "unjustified change and audit it with an empty reason"
            );

        await app.StopAsync();
    }

    /// <summary>
    /// The same rule from the other side: a body that is absent entirely must not reach a handler
    /// either. It is the case the filter was written for -- the handlers dereference their body.
    /// </summary>
    [Fact]
    public async Task EveryOperationRefusesAnAbsentBody()
    {
        await using WebApplication app = BuildApp();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        List<string> accepted = [];

        foreach (string route in OperationRoutes(app))
        {
            using StringContent empty = new("null", System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(route, empty);

            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                accepted.Add($"{route} -> {(int)response.StatusCode}");
            }
        }

        accepted.Should().BeEmpty();

        await app.StopAsync();
    }
}
