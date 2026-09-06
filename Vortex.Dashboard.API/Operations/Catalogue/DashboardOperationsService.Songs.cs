using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Sound.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Song catalogue admin operations. Each routes through
/// <see cref="Vortex.Primitives.Sound.ISongAdminService" /> — never a direct DB write — which
/// reloads the live catalogue after committing, so an added song is playable without a restart.
/// </summary>
/// <remarks>
/// Seconds in, milliseconds out: the operator types the length they read off the track, and the
/// conversion happens here rather than in the browser, so a page that forgets it cannot write a
/// song a thousand times too short.
/// </remarks>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateSongAsync(
        CreateSongRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.songs.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Name,
                request.Creator,
                request.LengthSeconds,
                request.OfficialSongId,
            },
            work: async c =>
                Throw(await _songAdmin.CreateAsync(SpecOf(request), c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> UpdateSongAsync(
        UpdateSongRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.songs.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.SongId,
                request.Name,
                request.Creator,
                request.LengthSeconds,
            },
            work: async c =>
                Throw(
                    await _songAdmin
                        .UpdateAsync(request.SongId, SpecOf(request), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteSongAsync(
        DeleteSongRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.songs.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.SongId },
            work: async c =>
                Throw(await _songAdmin.DeleteAsync(request.SongId, c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> ReloadSongsAsync(
        ReloadSongsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.songs.reload",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { },
            work: async c => Throw(await _songAdmin.ReloadAsync(c).ConfigureAwait(false)),
            ct
        );

    private static SongSpec SpecOf(CreateSongRequest request) =>
        new(
            request.Name,
            request.Creator ?? string.Empty,
            request.LengthSeconds * 1000,
            request.OfficialSongId ?? string.Empty,
            request.Data ?? string.Empty
        );

    private static SongSpec SpecOf(UpdateSongRequest request) =>
        new(
            request.Name,
            request.Creator ?? string.Empty,
            request.LengthSeconds * 1000,
            request.OfficialSongId ?? string.Empty,
            request.Data ?? string.Empty
        );

    private static void Throw(SongAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
