using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Rooms.Wired.Variables;

internal class WiredVariableFeatureProcessor(IRoomWiredVariablesProvider wiredVariablesProvider)
    : IAssemblyFeatureProcessor
{
    private readonly IRoomWiredVariablesProvider _wiredVariablesProvider = wiredVariablesProvider;

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        CompositeDisposable batch = new CompositeDisposable();

        foreach (
            Type? concrete in AssemblyExplorer.FindAssignees(asm, typeof(IWiredInternalVariable))
        )
        {
            if (concrete is null)
            {
                continue;
            }

            batch.Add(
                _wiredVariablesProvider.RegisterVariable(
                    sp,
                    (sp, ctx) =>
                        (IWiredVariable)ActivatorUtilities.CreateInstance(sp, concrete, ctx)
                )
            );
        }

        return Task.FromResult<IDisposable>(batch);
    }
}
