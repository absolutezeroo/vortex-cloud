using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Pipeline.Delegates;

public delegate ValueTask HandlerInvoker<TContext>(
    object inst,
    object env,
    TContext ctx,
    CancellationToken ct
);
