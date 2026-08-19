using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Logging.Extensions;
using Vortex.Primitives.Console;
using Xunit;

namespace Vortex.Hosting.Tests;

/// <summary>
/// The dashboard's console is a second sink onto the very lines the terminal prints, wired up by
/// <c>AddVortexLogging</c>. Exercised through that registration rather than against the provider
/// type: the wiring is the part that would silently stop working, and a page showing an empty
/// console looks exactly like a quiet server.
/// </summary>
public sealed class ServerConsoleLoggingTests : IDisposable
{
    private const char ESCAPE = '\u001b';

    private readonly List<IHost> _hosts = [];

    [Fact]
    public void ALoggedLine_ReachesTheConsoleFeed()
    {
        (ILogger logger, ServerConsoleFeed feed) = Logging();

        logger.LogInformation("Room {RoomId} is ready", 42);

        Lines(feed).Should().Contain(line => line.Contains("Room 42 is ready"));
    }

    [Fact]
    public void TheLine_CarriesItsCategory()
    {
        (ILogger logger, ServerConsoleFeed feed) = Logging(category: "Vortex.Rooms.RoomGrain");

        logger.LogWarning("something odd");

        Lines(feed).Should().Contain(line => line.Contains("RoomGrain"));
    }

    /// <summary>
    /// The formatter this borrows emits ANSI colour for the terminal, which is noise in a browser —
    /// the feed strips it on the way in.
    /// </summary>
    [Fact]
    public void TheLine_CarriesNoAnsiColour()
    {
        (ILogger logger, ServerConsoleFeed feed) = Logging();

        logger.LogError("a red line");

        Lines(feed).Should().OnlyContain(line => !line.Contains(ESCAPE));
    }

    [Fact]
    public void AnException_ArrivesAsItsOwnRowsRatherThanOneBlob()
    {
        (ILogger logger, ServerConsoleFeed feed) = Logging();

        logger.LogError(new InvalidOperationException("boom"), "it failed");

        IReadOnlyList<string> lines = Lines(feed);

        lines.Should().Contain(line => line.Contains("it failed"));
        lines.Should().Contain(line => line.Contains("boom"));
        lines.Should().OnlyContain(line => !line.Contains('\n'));
    }

    /// <summary>
    /// Level rules belong to the logging factory, not to this sink: answering them a second time
    /// here would silently override what the operator configured.
    /// </summary>
    [Fact]
    public void TheConfiguredLevelFilter_StillApplies()
    {
        (ILogger logger, ServerConsoleFeed feed) = Logging(minimumLevel: LogLevel.Warning);

        logger.LogInformation("beneath the floor");
        logger.LogWarning("above it");

        IReadOnlyList<string> lines = Lines(feed);

        lines.Should().Contain(line => line.Contains("above it"));
        lines.Should().NotContain(line => line.Contains("beneath the floor"));
    }

    private static IReadOnlyList<string> Lines(ServerConsoleFeed feed)
    {
        using ServerConsoleSubscription subscription = feed.Subscribe();

        return [.. subscription.Backlog];
    }

    private (ILogger Logger, ServerConsoleFeed Feed) Logging(
        string category = "Vortex.Tests",
        LogLevel minimumLevel = LogLevel.Trace
    )
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { EnvironmentName = Environments.Development }
        );

        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = minimumLevel.ToString(),
                // Colour is on in the shipped configuration, so leave it on here: stripping it is
                // part of what is under test.
                ["Logging:VortexConsole:UseAnsiColor"] = "true",
            }
        );

        builder.Services.AddLogging();
        builder.Services.AddVortexLogging(builder);

        IHost host = builder.Build();
        _hosts.Add(host);

        return (
            host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(category),
            host.Services.GetRequiredService<ServerConsoleFeed>()
        );
    }

    public void Dispose()
    {
        foreach (IHost host in _hosts)
        {
            host.Dispose();
        }
    }
}
