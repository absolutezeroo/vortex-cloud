using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Vortex.Dashboard.API.Hosting;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Every service the dashboard's request pipeline resolves for itself is one that is actually
/// forwarded into the web app's container.
/// </summary>
/// <remarks>
/// <para>
/// The dashboard runs as its own ASP.NET Core app with its own container, and the singletons it uses
/// are forwarded into it one by one from the parent. <c>DashboardEndpointServiceTests</c> covers the
/// types an <em>endpoint delegate</em> asks for. Nothing covered the ones <c>ConfigurePipeline</c>
/// asks for directly, and the failure mode is not subtle: the dashboard throws while building its
/// pipeline and the whole admin surface fails to start, degraded, on one line in a log.
/// </para>
/// <para>
/// That is exactly how <c>IVortexContextAccessor</c> shipped. The correlation middleware resolved it,
/// nobody forwarded it, and the emulator came up without a dashboard.
/// </para>
/// <para>
/// Both sides are read out of the source. An allow-list of "types I know are handled" was the first
/// version of this test and it could not fail: deleting the forwarding left the name on the list and
/// the test green. What makes it a test is that the forwarding it checks against is the forwarding
/// that is actually written.
/// </para>
/// </remarks>
public sealed class DashboardPipelineServiceTests
{
    [Fact]
    public void EverythingThePipelineResolvesIsForwardedIntoItsContainer()
    {
        string source = File.ReadAllText(HostSourcePath());

        // Resolutions against the web app's own container. `rootServices.GetRequiredService` reads
        // the parent, which by definition has everything, and is what does the forwarding.
        IReadOnlyList<string> resolved =
        [
            .. Regex
                .Matches(source, @"app\.Services\.GetRequiredService<(\w+)>")
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.Ordinal),
        ];

        resolved
            .Should()
            .NotBeEmpty("the pipeline resolves at least the asset urls and the audit emitter");

        // Read, not listed: the loop over ForwardedServiceTypes, plus every explicit
        // `AddSingleton(rootServices.GetRequiredService<T>())` beside it.
        HashSet<string> forwarded = new(
            Regex
                .Matches(source, @"AddSingleton\(rootServices\.GetRequiredService<(\w+)>\(\)\)")
                .Select(m => m.Groups[1].Value),
            StringComparer.Ordinal
        );

        foreach (Type type in DashboardWebHost.ForwardedServiceTypes)
        {
            forwarded.Add(type.Name);
        }

        IReadOnlyList<string> missing =
        [
            .. resolved.Where(name => !forwarded.Contains(name)).Order(StringComparer.Ordinal),
        ];

        missing
            .Should()
            .BeEmpty(
                "a service the pipeline resolves but nobody forwards makes the dashboard fail to "
                    + "start, and the emulator carries on degraded without an admin surface"
            );
    }

    private static string HostSourcePath()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);

        while (
            dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "Vortex.Dashboard.API"))
        )
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the repository root is above the test binary");

        return Path.Combine(
            dir!.FullName,
            "Vortex.Dashboard.API",
            "Hosting",
            "DashboardWebHost.cs"
        );
    }
}
