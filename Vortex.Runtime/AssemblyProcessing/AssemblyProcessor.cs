using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Runtime.AssemblyProcessing;

public sealed class AssemblyProcessor(IEnumerable<IAssemblyFeatureProcessor> processors)
{
    private readonly IReadOnlyList<IAssemblyFeatureProcessor> _processors = [.. processors];

    public async Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        Task<IDisposable>[] tasks = _processors.Select(p => p.ProcessAsync(asm, sp, ct)).ToArray();

        try
        {
            IDisposable[] regs = await Task.WhenAll(tasks).ConfigureAwait(false);

            return new CompositeDisposable(regs.AsEnumerable().Reverse());
        }
        catch
        {
            foreach (Task<IDisposable> t in tasks)
            {
                if (t.IsCompletedSuccessfully)
                {
                    (await t.ConfigureAwait(false)).Dispose();
                }
            }

            throw;
        }
    }
}
