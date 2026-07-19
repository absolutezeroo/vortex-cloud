using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Pipeline.Delegates;

public delegate ValueTask BehaviorInvoker<TContext>(
    object inst,
    object env,
    TContext ctx,
    Func<ValueTask> next,
    CancellationToken ct
);
