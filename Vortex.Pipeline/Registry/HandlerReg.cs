using System;
using Vortex.Pipeline.Delegates;

namespace Vortex.Pipeline.Registry;

internal sealed record HandlerReg<TContext>(
    IServiceProvider ServiceProvider,
    Func<IServiceProvider, object> Activator,
    HandlerInvoker<TContext> Invoker
);
