using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Operations;
using Vortex.Database.Backup;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The coarse safety net: list the database dumps that exist, and ask for one now.
///
/// <para>
/// Read-only over the schedule on purpose. Where it writes, how often and how many are kept stay in
/// bootstrap configuration — a backup whose retention can be set to zero from the same dashboard
/// whose mistakes it exists to undo is not a safety net. Restoring is deliberately absent too:
/// rolling the whole database back to a dump discards every change since, which is a decision for a
/// shell and a maintenance window, not a button next to the one that caused the problem.
/// </para>
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagBackup = "Backup";
    private const string ApiBackup = ApiV1 + "/database/backups";

    public static void MapBackupReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiBackup,
            (IDatabaseBackupService backups, CancellationToken ct) =>
                OkAsync(
                    Task.FromResult<object>(
                        new
                        {
                            configured = backups.IsConfigured,
                            items = backups
                                .List()
                                .Select(b => new
                                {
                                    b.FileName,
                                    b.SizeBytes,
                                    b.CreatedUtc,
                                })
                                .ToArray(),
                        }
                    )
                ),
            Capabilities.Dashboard.OpsDatabaseBackup,
            TagBackup
        );
    }

    public static void MapBackupOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/database/backup",
            async (
                HttpContext ctx,
                CreateDatabaseBackupRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.CreateDatabaseBackupAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsDatabaseBackup,
            TagBackup
        );
    }
}
