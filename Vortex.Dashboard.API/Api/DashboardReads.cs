using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// What every read class needs and none of them decides: a context, opened and disposed.
/// </summary>
/// <remarks>
/// <para>
/// A base class rather than an injected helper, which is the opposite of the choice made for
/// <see cref="Operations.OperationRunner"/>, and for a reason. The runner carries policy — auditing,
/// correlation, what a failure means — so a subject must not be able to inherit or bend it. This
/// carries none: it opens a context, runs the caller's function, disposes it. Injecting a
/// collaborator to do that would be a field and a constructor parameter on thirty classes to save
/// eight lines each.
/// </para>
/// <para>
/// It is deliberately the whole of the base. Anything a read class needs beyond a context — an asset
/// URL builder, the observability config, a grain factory — it declares itself, which is how a
/// subject's real dependencies stay visible in its constructor instead of being inherited from a
/// service that took ten for everyone.
/// </para>
/// </remarks>
internal abstract class DashboardReads(IDbContextFactory<VortexDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    /// <summary>Opens a context for one read and disposes it, whatever the read does.</summary>
    protected async Task<T> QueryAsync<T>(Func<VortexDbContext, Task<T>> work, CancellationToken ct)
    {
        VortexDbContext db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            return await work(db).ConfigureAwait(false);
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }
}
