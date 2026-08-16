using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Hosting;
using Vortex.Observability.Configuration;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// Guards the one mistake that turns a new dashboard endpoint into a dead dashboard.
///
/// <para>
/// The dashboard API runs in its own <see cref="WebApplication" /> whose container holds only what
/// <see cref="DashboardWebHost.ForwardedServiceTypes" /> lists. Minimal APIs classify a handler
/// parameter by asking that container whether it recognises the type; anything it does not
/// recognise is taken for the JSON request body. A GET cannot have one, so the omission does not
/// produce a broken route — it throws while ASP.NET Core is building the pipeline, before a single
/// request is served, and the whole dashboard falls to DEGRADED with "Body was inferred but the
/// method does not allow inferred body parameters".
/// </para>
///
/// <para>
/// Neither the compiler nor the quality gate sees any of that: injecting an unforwarded service
/// compiles perfectly. So this test builds the same container the host builds and forces the
/// endpoint metadata inference that fails at startup, which is the only cheap place the mistake is
/// visible. It caught <c>IDatabaseBackupService</c> on GET /api/database/backups.
/// </para>
/// </summary>
public sealed class DashboardEndpointServiceTests
{
    [Fact]
    public void EveryEndpointParameterIsEitherAForwardedServiceOrABodyTheMethodAllows()
    {
        WebApplication app = BuildAppWithForwardedServicesOnly();

        DashboardEndpoints.MapAuth(app);
        DashboardEndpoints.MapReadApi(app, () => DateTime.UnixEpoch);
        DashboardEndpoints.MapOperations(app);
        DashboardEndpoints.MapMeta(app);
        DashboardEndpoints.MapFrontend(app);

        List<Endpoint> endpoints = [];

        // Reading a data source's endpoints is what runs parameter inference over every handler it
        // holds, and is exactly the step DashboardWebHost reaches through app.StartAsync() — there
        // by way of the authorization middleware, which touches the composite source as the
        // pipeline is assembled.
        Action inferEveryEndpoint = () =>
            endpoints = [
                .. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints),
            ];

        inferEveryEndpoint.Should().NotThrow();
        endpoints.Should().NotBeEmpty();
    }

    [Fact]
    public void ForwardedServicesAreDistinct()
    {
        DashboardWebHost.ForwardedServiceTypes.Should().OnlyHaveUniqueItems().And.NotContainNulls();
    }

    /// <summary>
    /// Mirrors <c>DashboardWebHost.ForwardSingletons</c> without the parent container: the forwarded
    /// types are registered through factories that would throw if anything resolved them. Nothing
    /// does — inference only asks whether the container knows a type, and the handlers themselves
    /// never run here — so the real services (which need Orleans and a live database) stay out of a
    /// unit test while remaining indistinguishable from the host's registrations to the inference.
    /// </summary>
    private static WebApplication BuildAppWithForwardedServicesOnly()
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
}
