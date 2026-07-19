using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Runtime.AssemblyProcessing;

public interface IAssemblyFeatureProcessor
{
    Task<IDisposable> ProcessAsync(
        Assembly assembly,
        IServiceProvider pluginServices,
        CancellationToken ct = default
    );
}
