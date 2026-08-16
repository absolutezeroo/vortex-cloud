using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Database.Backup;

namespace Vortex.Dashboard.API.Operations;

internal sealed partial class DashboardOperationsService
{
    /// <summary>
    /// Takes a dump on demand, on top of whatever the schedule does. Audited like any other
    /// operation: an extra backup is usually taken right before something risky, and knowing which
    /// one that was is half of why it is worth having.
    /// </summary>
    public Task<OperationResult> CreateDatabaseBackupAsync(
        CreateDatabaseBackupRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.database.backup",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { trigger = "manual" },
            work: async c =>
            {
                DatabaseBackupResult result = await _databaseBackups
                    .CreateAsync("manual", c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Message);
                }
            },
            ct
        );
}
