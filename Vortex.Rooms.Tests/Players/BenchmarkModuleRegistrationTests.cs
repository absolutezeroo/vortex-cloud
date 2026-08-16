using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Benchmark;
using Vortex.Observability.Runtime;
using Xunit;

namespace Vortex.Rooms.Tests.Players;

/// <summary>
/// Every service the benchmark module builds must have its dependencies registered by that same
/// module.
/// </summary>
/// <remarks>
/// <para>
/// This exists because the compiler does not check it and neither did anything else. A service added
/// to a constructor but never registered compiles, passes every test, and then takes the whole host
/// down at boot with <c>Unable to resolve service for type X</c> — the container validates on
/// build, which is the first moment anybody finds out.
/// </para>
/// <para>
/// The suite that would have caught it is <c>Vortex.Hosting.Tests</c>, which builds the real host;
/// it cannot run while the emulator holds <c>Vortex.Main</c>'s output, which is exactly when someone
/// is most likely to be editing this. So the same question is asked here, of this module alone,
/// where no lock can stop it.
/// </para>
/// </remarks>
public sealed class BenchmarkModuleRegistrationTests
{
    /// <summary>
    /// What the host supplies rather than the module: framework services, and the singletons other
    /// modules register. Anything outside this list has to come from the module itself.
    /// </summary>
    private static readonly HashSet<Type> ProvidedByTheHost =
    [
        typeof(IGrainFactory),
        typeof(IConfiguration),
        typeof(IHostEnvironment),
        typeof(RoomPerformanceAggregator),
        typeof(IDbContextFactory<Vortex.Database.Context.VortexDbContext>),
    ];

    [Fact]
    public void EveryDependencyTheModuleNeeds_IsRegisteredOrProvidedByTheHost()
    {
        ServiceCollection services = [];

        new BenchmarkModule().ConfigureServices(services, Host.CreateApplicationBuilder());

        HashSet<Type> registered =
        [
            .. services.Select(descriptor => descriptor.ServiceType),
            .. services.Select(descriptor => descriptor.ImplementationType).OfType<Type>(),
        ];

        List<string> missing = [];

        foreach (Type implementation in services.Select(d => d.ImplementationType).OfType<Type>())
        {
            ConstructorInfo? constructor = implementation
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            foreach (ParameterInfo parameter in constructor?.GetParameters() ?? [])
            {
                Type needed = parameter.ParameterType;

                // Loggers are open-generic and always available.
                if (needed.IsGenericType && needed.GetGenericTypeDefinition() == typeof(ILogger<>))
                {
                    continue;
                }

                if (registered.Contains(needed) || ProvidedByTheHost.Contains(needed))
                {
                    continue;
                }

                missing.Add($"{implementation.Name} needs {needed.Name}");
            }
        }

        missing.Should().BeEmpty("the host validates its container at boot and refuses to start");
    }
}
