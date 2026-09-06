using System;
using Microsoft.Extensions.DependencyInjection;

namespace Vortex.Primitives.Plugins;

/// <summary>
/// How a plugin borrows a service from the host it is loaded into.
/// </summary>
/// <remarks>
/// <para>
/// A plugin's container is built empty — the manifest, the export catalogue,
/// <see cref="IHostServices"/> and logging, and then whatever the plugin registers. Nothing of the
/// host's is in it, which is correct: a plugin that could reach anything by accident would not be a
/// boundary at all. But it means a plugin of any size cannot use constructor injection for the
/// things it genuinely needs from the hotel, and rewriting every constructor to call
/// <see cref="IHostServices.GetRequiredService{T}"/> by hand is not a boundary either, only a
/// tax.
/// </para>
/// <para>
/// So a plugin says what it borrows, once, and then injects it normally. The list that results is
/// the useful artefact: it is exactly the contract between that plugin and the hotel, readable in
/// one place, and <c>PluginManager</c> checks every entry of it at activation rather than letting
/// a missing service surface on the first request that happens to need it.
/// </para>
/// </remarks>
public static class HostServiceExtensions
{
    /// <summary>
    /// Makes the host's <typeparamref name="T"/> injectable inside the plugin.
    /// </summary>
    /// <remarks>
    /// Resolved through <see cref="IHostServices"/> rather than captured, so the plugin never holds
    /// the host's provider and cannot enumerate it. Registered as a singleton because the host's own
    /// registration decides the lifetime; asking twice must not produce two of the hotel's things.
    /// </remarks>
    public static IServiceCollection UseHostService<T>(this IServiceCollection services)
        where T : class
    {
        services.AddSingleton(
            typeof(HostServiceRequirement),
            new HostServiceRequirement(typeof(T))
        );

        return services.AddSingleton(sp =>
            sp.GetRequiredService<IHostServices>().GetRequiredService<T>()
        );
    }

    /// <summary>
    /// One entry of a plugin's borrowing list, recorded so activation can verify it.
    /// </summary>
    /// <remarks>
    /// Without this the factory above is lazy: a service the host does not have fails whenever it is
    /// first resolved, which for a web front-end is the first request that needs it — long after
    /// anybody was watching the load.
    /// </remarks>
    public sealed record HostServiceRequirement(Type ServiceType);
}
