using System;
using Vortex.Pipeline.Delegates;

namespace Vortex.Pipeline.Registry;

internal sealed record BehaviorReg<TContext>(
    IServiceProvider ServiceProvider,
    int Order,
    Func<IServiceProvider, object> Activator,
    BehaviorInvoker<TContext> Invoker
);
