using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Gamedata;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Sound.Providers;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Furniture.Providers;

/// <summary>
/// Loads the hotel's songs once and serves them from memory.
/// </summary>
/// <remarks>
/// Reference data, the same shape as <see cref="FurnitureDefinitionProvider" />: read constantly —
/// the client asks about every unknown song id it meets — and written only by an operator reload. It
/// lives beside the furniture providers because a song is only ever reached through furniture: a
/// song disk carries the id, a jukebox plays it, a catalogue offer sells it.
/// </remarks>
public sealed class SongProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IVortexMetrics metrics,
    ILogger<ISongProvider> logger
) : ISongProvider, IReferenceDataProvider
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IVortexMetrics _metrics = metrics;
    private readonly ILogger<ISongProvider> _logger = logger;

    // Nothing else reads songs while it loads, so the first stage is as good as any.
    public int LoadStage => 0;

    /// <summary>
    /// Both indexes and the version they were built at, published as one object so a reader can
    /// never see the new songs by id and the old ones by official code.
    /// </summary>
    private sealed record SongSet
    {
        public required ImmutableDictionary<int, SongSnapshot> ById { get; init; }

        /// <summary>Keyed by the external code. Songs composed in-hotel have none, and duplicates
        /// are possible in hand-filled data, so the index is built from the first of each.</summary>
        public required ImmutableDictionary<string, SongSnapshot> ByOfficialId { get; init; }

        public required int Version { get; init; }

        public static readonly SongSet Empty = new()
        {
            ById = ImmutableDictionary<int, SongSnapshot>.Empty,
            ByOfficialId = ImmutableDictionary<string, SongSnapshot>.Empty,
            Version = 0,
        };
    }

    private SongSet _songs = SongSet.Empty;

    public SongSnapshot? TryGetSong(int id) =>
        _songs.ById.TryGetValue(id, out SongSnapshot? song) ? song : null;

    public SongSnapshot? TryGetSongByOfficialId(string officialSongId) =>
        string.IsNullOrEmpty(officialSongId) ? null
        : _songs.ByOfficialId.TryGetValue(officialSongId, out SongSnapshot? song) ? song
        : null;

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<SongEntity> entities = await dbCtx
            .Songs.AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<SongSnapshot> songs =
        [
            .. entities.Select(s => new SongSnapshot
            {
                Id = s.Id,
                Name = s.Name,
                Creator = s.Creator,
                LengthMs = s.LengthMs,
                OfficialSongId = s.OfficialSongId,
                Data = s.Data,
            }),
        ];

        SongSet published = new()
        {
            ById = songs.ToImmutableDictionary(s => s.Id),
            ByOfficialId = songs
                .Where(s => !string.IsNullOrEmpty(s.OfficialSongId))
                .DistinctBy(s => s.OfficialSongId)
                .ToImmutableDictionary(s => s.OfficialSongId),
            Version = _songs.Version + 1,
        };

        Volatile.Write(ref _songs, published);

        _metrics.ReferenceDataPublished(nameof(SongProvider), published.Version);

        _logger.LogInformation(
            "Loaded {SongCount} songs (version {Version})",
            published.ById.Count,
            published.Version
        );
    }
}
