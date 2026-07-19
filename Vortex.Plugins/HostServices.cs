using System;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Primitives.Plugins;

namespace Vortex.Plugins;

internal sealed class HostServices(IServiceProvider host) : IHostServices
{
    private readonly IServiceProvider _host = host;

    public T GetRequiredService<T>()
        where T : notnull => _host.GetRequiredService<T>();
}
