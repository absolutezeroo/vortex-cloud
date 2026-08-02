using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Observability;
using Xunit;

namespace Vortex.WebApi.Tests;

/// <summary>
/// The scraping endpoint's contract: it exists only when switched on, it is reachable only by callers
/// the configuration lets in, and when it does answer it renders the process's "Vortex" meter in
/// Prometheus' text format.
/// </summary>
public sealed class MetricsScrapingEndpointTests
{
    private const string Token = "an-operator-supplied-scrape-token";

    /// <summary>An address outside 127.0.0.0/8 and not ::1, i.e. a scrape arriving off the box.</summary>
    private static readonly IPAddress RemoteCaller = IPAddress.Parse("203.0.113.9");

    [Fact]
    public async Task WhenDisabled_TheEndpointIsNotThere()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory();

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenEnabledWithoutToken_ALoopbackScrapeRendersTheVortexMeter()
    {
        // A meter created here is the same process-global meter the emulator's instruments use, so
        // this proves the exporter is actually subscribed to it rather than to some local copy.
        using Meter meter = new Meter(VortexMeterNames.VORTEX, "1.0.0");
        Counter<long> probe = meter.CreateCounter<long>("Vortex.test.scrape_probe");

        await using WebApiTestFactory factory = new WebApiTestFactory(config =>
            config.MetricsEnabled = true
        );

        probe.Add(3);

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Vortex_test_scrape_probe");
    }

    [Fact]
    public async Task WhenEnabledWithoutToken_AnOffBoxScrapeIsNotAcknowledged()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(
            config => config.MetricsEnabled = true,
            RemoteCaller
        );

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        // 404 rather than 403: with no token configured the endpoint is local-only, and an off-box
        // caller should not learn it is there at all.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenATokenIsConfigured_AnUnauthenticatedScrapeIsRejectedEvenFromLoopback()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(config =>
        {
            config.MetricsEnabled = true;
            config.MetricsToken = Token;
        });

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Contain("Bearer");
    }

    [Fact]
    public async Task WhenATokenIsConfigured_AWrongTokenIsRejected()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(config =>
        {
            config.MetricsEnabled = true;
            config.MetricsToken = Token;
        });

        factory.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Token + "-not-quite"
        );

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenATokenIsConfigured_AnOffBoxScrapeCarryingItIsServed()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(
            config =>
            {
                config.MetricsEnabled = true;
                config.MetricsToken = Token;
            },
            RemoteCaller
        );

        factory.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Token
        );

        HttpResponseMessage response = await factory.Client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheConfiguredPathIsHonouredAndNothingElseIsExposed()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(config =>
        {
            config.MetricsEnabled = true;
            config.MetricsPath = "/internal/telemetry";
        });

        HttpResponseMessage onPath = await factory.Client.GetAsync("/internal/telemetry");
        HttpResponseMessage onDefault = await factory.Client.GetAsync("/metrics");

        onPath.StatusCode.Should().Be(HttpStatusCode.OK);
        onDefault.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EnablingScrapingLeavesTheRestOfTheApiAlone()
    {
        await using WebApiTestFactory factory = new WebApiTestFactory(config =>
            config.MetricsEnabled = true
        );

        HttpResponseMessage health = await factory.Client.GetAsync("/health");

        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
