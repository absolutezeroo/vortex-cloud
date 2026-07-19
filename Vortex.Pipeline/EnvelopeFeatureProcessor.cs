using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Pipeline.Attributes;
using Vortex.Pipeline.Delegates;
using Vortex.Runtime;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Pipeline;

public class EnvelopeFeatureProcessor<TEnvelope, TMeta, TContext>(
    EnvelopeHost<TEnvelope, TMeta, TContext> registry,
    EnvelopeInvokerFactory<TContext> invokerFactory,
    Type openHandlerInterface,
    Type openBehaviorInterface
) : IAssemblyFeatureProcessor
{
    private readonly EnvelopeHost<TEnvelope, TMeta, TContext> _registry = registry;
    private readonly EnvelopeInvokerFactory<TContext> _invokerFactory = invokerFactory;
    private readonly Type _openHandlerInterface = openHandlerInterface;
    private readonly Type _openBehaviorInterface = openBehaviorInterface;

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
            ) in AssemblyExplorer.FindClosedImplementations(asm, _openHandlerInterface)
        )
        {
            Type envType = args[0];
            HandlerInvoker<TContext> invoker = _invokerFactory.CreateHandlerInvoker(
                concrete,
                envType
            );
            Func<IServiceProvider, object> activator = ActivatorHelpers.BuildActivator(concrete);

            batch.Add(_registry.RegisterHandler(envType, sp, activator, invoker));
        }

        foreach (
            (
                Type concrete,
                Type closedIface,
                Type[] args
            ) in AssemblyExplorer.FindClosedImplementations(asm, _openBehaviorInterface)
        )
        {
            Type envType = args[0];
            BehaviorInvoker<TContext> invoker = _invokerFactory.CreateBehaviorInvoker(
                concrete,
                envType
            );
            int order = concrete.GetCustomAttribute<OrderAttribute>()?.Value ?? 0;
            Func<IServiceProvider, object> activator = ActivatorHelpers.BuildActivator(concrete);

            batch.Add(_registry.RegisterBehavior(envType, sp, activator, invoker, order));
        }

        return Task.FromResult<IDisposable>(batch);
    }
}
