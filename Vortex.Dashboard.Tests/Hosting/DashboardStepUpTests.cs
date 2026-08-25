using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
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
using Vortex.Dashboard.API.Security;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Step-up MFA: a valid login is not, by itself, enough to run a critical operation.
/// </summary>
/// <remarks>
/// <para>
/// The frozen note's rule is that step-up belongs to the operator's security context and not to the
/// payload the browser sends, so every case here is driven through the cookie and the session store —
/// there is no field a request could set to claim it had stepped up, and these tests would have no
/// way to write one.
/// </para>
/// <para>
/// The rule that most needs a test is the freshness comparison. Inverted, it would let every session
/// through forever, and nothing else in the system would say so: the operations still succeed, still
/// audit, still look exactly right.
/// </para>
/// </remarks>
public sealed class DashboardStepUpTests
{
    private const int ACCOUNT = 42;
    private const string EMAIL = "operator@vortex.test";

    [Fact]
    public async Task WithTheWindowAtZeroNothingIsAsked()
    {
        // The shipped default. An installation whose operators have not enrolled a factor keeps
        // working exactly as it did.
        await using Harness h = await Harness.StartAsync(windowMinutes: 0, enrolled: false);

        (await h.PostCriticalAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ASessionThatHasNotSteppedUpIsRefused()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: true);

        HttpResponseMessage response = await h.PostCriticalAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.ErrorAsync(response)).Should().Be("mfa_step_up_required");
    }

    [Fact]
    public async Task ASessionThatHasJustSteppedUpIsLetThrough()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: true);

        h.Sessions.MarkSteppedUp(h.SessionId);

        (await h.PostCriticalAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// The one an inverted comparison would break silently: a step-up older than the window is not a
    /// step-up. Without this, "recent" would mean "ever".
    /// </summary>
    [Fact]
    public async Task AStepUpOlderThanTheWindowIsNotAStepUp()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: true);

        h.Sessions.MarkSteppedUp(h.SessionId, DateTime.UtcNow.AddMinutes(-16));

        HttpResponseMessage response = await h.PostCriticalAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.ErrorAsync(response)).Should().Be("mfa_step_up_required");
    }

    [Fact]
    public async Task AStepUpInsideTheWindowStillCounts()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: true);

        h.Sessions.MarkSteppedUp(h.SessionId, DateTime.UtcNow.AddMinutes(-14));

        (await h.PostCriticalAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// An operator with no factor gets a different code, because no code dialog can help them. Told
    /// the same thing as everyone else, they would sit retrying a prompt that can never succeed.
    /// </summary>
    [Fact]
    public async Task AnOperatorWithNoFactorIsToldToEnrolInstead()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: false);

        HttpResponseMessage response = await h.PostCriticalAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.ErrorAsync(response)).Should().Be("mfa_enrolment_required");
    }

    /// <summary>
    /// A route whose capability is not on the critical list is untouched — otherwise a passing suite
    /// above would be consistent with a filter that simply refuses everything.
    /// </summary>
    [Fact]
    public async Task AnOrdinaryOperationIsNeverAsked()
    {
        await using Harness h = await Harness.StartAsync(windowMinutes: 15, enrolled: true);

        (await h.PostOrdinaryAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Every capability the list names has to be one the dashboard actually declares. A typo here
    /// would produce a set entry that matches no route, and the operations it was meant to protect
    /// would carry on unprotected with nothing to show for it.
    /// </summary>
    [Fact]
    public void EveryCriticalCapabilityIsARealOne()
    {
        StepUpRequired
            .Capabilities.Should()
            .OnlyContain(capability => Capabilities.Dashboard.All.Contains(capability));
    }

    private sealed class Harness : IAsyncDisposable
    {
        private const string CRITICAL = Capabilities.Dashboard.OpsGrantCurrency;
        private const string ORDINARY = Capabilities.Dashboard.OpsRoomsManage;

        private readonly WebApplication _app;
        private readonly HttpClient _client;
        private readonly DummyMeterFactory _meters;

        public DashboardSessionStore Sessions { get; }

        public string SessionId { get; }

        private Harness(
            WebApplication app,
            HttpClient client,
            DashboardSessionStore sessions,
            string sessionId,
            DummyMeterFactory meters
        )
        {
            _app = app;
            _client = client;
            Sessions = sessions;
            SessionId = sessionId;
            _meters = meters;
        }

        public static async Task<Harness> StartAsync(int windowMinutes, bool enrolled)
        {
            ObservabilityConfig config = new()
            {
                MetricsEnabled = false,
                DashboardStepUpMinutes = windowMinutes,
            };

            IOptions<ObservabilityConfig> options = Options.Create(config);
            DummyMeterFactory meters = new();
            DashboardSessionStore sessions = new(options, meters);
            string sessionId = sessions.Create(ACCOUNT, EMAIL);

            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(sessions);
            builder.Services.AddSingleton<IAccountMfaService>(new StubMfa(enrolled));
            builder
                .Services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, StubOperator>("Test", null);
            builder.Services.AddAuthorizationBuilder();

            WebApplication app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            // Mapped the way the real routes are, so the filter is attached by the same decision the
            // dashboard makes: from the capability, not from a flag written here.
            Map(app, "/api/v1/operations/critical", CRITICAL);
            Map(app, "/api/v1/operations/ordinary", ORDINARY);

            await app.StartAsync();

            HttpClient client = app.GetTestClient();
            client.DefaultRequestHeaders.Add(
                "Cookie",
                $"{DashboardAuthenticationHandler.SessionCookieName}={sessionId}"
            );

            return new Harness(app, client, sessions, sessionId, meters);
        }

        private static void Map(WebApplication app, string path, string capability)
        {
            RouteHandlerBuilder route = app.MapPost(path, () => Results.Ok(new { ok = true }))
                .RequireAuthorization();

            if (StepUpRequired.Capabilities.Contains(capability))
            {
                route
                    .WithMetadata(StepUpRequired.Instance)
                    .AddEndpointFilter<DashboardStepUpFilter>();
            }
        }

        public Task<HttpResponseMessage> PostCriticalAsync() =>
            _client.PostAsJsonAsync("/api/v1/operations/critical", new { reason = "testing" });

        public Task<HttpResponseMessage> PostOrdinaryAsync() =>
            _client.PostAsJsonAsync("/api/v1/operations/ordinary", new { reason = "testing" });

        public static async Task<string?> ErrorAsync(HttpResponseMessage response)
        {
            using JsonDocument body = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync()
            );

            return body.RootElement.GetProperty("error").GetString();
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            Sessions.Dispose();
            _meters.Dispose();
            await _app.DisposeAsync();
        }
    }

    /// <summary>Signs every request in as the operator whose session the harness minted.</summary>
    private sealed class StubOperator(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            System.Security.Claims.ClaimsIdentity identity = new(
                [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, EMAIL)],
                "Test"
            );

            // The filter reads the rich principal from Items, exactly as the real handler leaves it.
            Context.Items[DashboardAuthenticationHandler.PrincipalItemKey] = new DashboardPrincipal(
                ACCOUNT,
                EMAIL,
                new PermissionSet([], [Capabilities.Wildcard])
            );

            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new System.Security.Claims.ClaimsPrincipal(identity),
                        "Test"
                    )
                )
            );
        }
    }

    private sealed class StubMfa(bool enrolled) : IAccountMfaService
    {
        public Task<bool> IsEnabledAsync(int accountId, CancellationToken ct = default) =>
            Task.FromResult(enrolled);

        public Task<MfaEnrolment> BeginEnrolmentAsync(
            int accountId,
            CancellationToken ct = default
        ) => throw new NotSupportedException();

        public Task<bool> ConfirmEnrolmentAsync(
            int accountId,
            string secret,
            string code,
            CancellationToken ct = default
        ) => throw new NotSupportedException();

        public Task<bool> VerifyAsync(
            int accountId,
            string? code,
            CancellationToken ct = default
        ) => Task.FromResult(enrolled && code == "000000");

        public Task<bool> DisableAsync(
            int accountId,
            string? code,
            CancellationToken ct = default
        ) => throw new NotSupportedException();
    }

    /// <summary>The store creates a meter only when metrics are on, which the harness turns off.</summary>
    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose() { }
    }
}
