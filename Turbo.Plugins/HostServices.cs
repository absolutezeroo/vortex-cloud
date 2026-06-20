using System;
using Microsoft.Extensions.DependencyInjection;
using Turbo.Primitives.Plugins;

namespace Turbo.Plugins;

internal sealed class HostServices(IServiceProvider host) : IHostServices
{
    private readonly IServiceProvider _host = host;

    public T GetRequiredService<T>()
        where T : notnull => _host.GetRequiredService<T>();
}
