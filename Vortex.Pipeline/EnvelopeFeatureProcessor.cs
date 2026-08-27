using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Pipeline.Attributes;
using Vortex.Pipeline.Delegates;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Pipeline;

public class EnvelopeFeatureProcessor<TEnvelope, TMeta, TContext>(
    EnvelopeHost<TEnvelope, TMeta, TContext> registry,
    EnvelopeInvokerFactory<TContext> invokerFactory,
    Type openHandlerInterface,
    Type openBehaviorInterface,
    ILogger logger
) : IAssemblyFeatureProcessor
{
    private readonly EnvelopeHost<TEnvelope, TMeta, TContext> _registry = registry;
    private readonly EnvelopeInvokerFactory<TContext> _invokerFactory = invokerFactory;
    private readonly Type _openHandlerInterface = openHandlerInterface;
    private readonly Type _openBehaviorInterface = openBehaviorInterface;
    private readonly ILogger _logger = logger;

    public Task<IDisposable> ProcessAsync(
        Assembly asm,
        IServiceProvider sp,
        CancellationToken ct = default
    )
    {
        CompositeDisposable batch = new CompositeDisposable();

        foreach (
            (
                Type concrete,
                Type closedIface,
                Type[] args
            ) in AssemblyExplorer.FindClosedImplementations(
                asm,
                _openHandlerInterface,
                WarnNonPublic
            )
        )
        {
            Type envType = args[0];
            HandlerInvoker<TContext> invoker = _invokerFactory.CreateHandlerInvoker(
                concrete,
                envType
            );
            Func<IServiceProvider, object> activator = ActivatorHelpers.BuildActivator(concrete);

            // CA2000: ownership transfers to `batch`, which is returned as this method's IDisposable
            // and disposed by the caller (AssemblyProcessor). The analyzer cannot follow ownership
            // into a collection.
#pragma warning disable CA2000
            batch.Add(_registry.RegisterHandler(envType, sp, activator, invoker));
#pragma warning restore CA2000
        }

        foreach (
            (
                Type concrete,
                Type closedIface,
                Type[] args
            ) in AssemblyExplorer.FindClosedImplementations(
                asm,
                _openBehaviorInterface,
                WarnNonPublic
            )
        )
        {
            Type envType = args[0];
            BehaviorInvoker<TContext> invoker = _invokerFactory.CreateBehaviorInvoker(
                concrete,
                envType
            );
            int order = concrete.GetCustomAttribute<OrderAttribute>()?.Value ?? 0;
            Func<IServiceProvider, object> activator = ActivatorHelpers.BuildActivator(concrete);

            // CA2000: same ownership transfer to `batch` as the handler registration above.
#pragma warning disable CA2000
            batch.Add(_registry.RegisterBehavior(envType, sp, activator, invoker, order));
#pragma warning restore CA2000
        }

        return Task.FromResult<IDisposable>(batch);
    }

    /// <summary>
    /// A non-public handler or behaviour compiles, ships, and is never registered — the scan takes
    /// public types only. Silence there is indistinguishable from "my handler never runs", so it is
    /// reported instead of dropped.
    /// </summary>
    private void WarnNonPublic(Type concrete) =>
        _logger.LogWarning(
            "{Type} implements a scanned pipeline interface but is not public, so it was not "
                + "registered and will never run. Make the type public.",
            concrete.FullName
        );
}
