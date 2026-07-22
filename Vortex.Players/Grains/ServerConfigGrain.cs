using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Server;
using Vortex.Primitives.Server.Grains;

namespace Vortex.Players.Grains;

/// <summary>
/// Single-activation config store. Caches all rows in memory on activation and keeps the cache
/// authoritative via write-through in <see cref="SetValueAsync"/>, so an admin edit is live for
/// every reader immediately (no reload dance) — including across a multi-node cluster, since Orleans
/// guarantees one activation.
/// </summary>
internal sealed class ServerConfigGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<ServerConfigGrain> logger
) : Grain, IServerConfigGrain
{
    private const string MotdKey = "motd.lines";

    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<ServerConfigGrain> _logger = logger;

    private readonly Dictionary<string, string> _cache = new();

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public Task<string?> GetValueAsync(string key) =>
        Task.FromResult(_cache.TryGetValue(key, out string? value) ? value : null);

    public Task<int> GetIntAsync(string key, int fallback) =>
        Task.FromResult(
            _cache.TryGetValue(key, out string? value) && int.TryParse(value, out int parsed)
                ? parsed
                : fallback
        );

    public Task<bool> GetBoolAsync(string key, bool fallback) =>
        Task.FromResult(
            _cache.TryGetValue(key, out string? value) && bool.TryParse(value, out bool parsed)
                ? parsed
                : fallback
        );

    public async Task SetValueAsync(string key, string value, string? description)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync()
            .ConfigureAwait(true);

        ServerConfigEntity? entity = await dbCtx
            .ServerConfig.FirstOrDefaultAsync(c => c.Key == key)
            .ConfigureAwait(true);

        if (entity is null)
        {
            dbCtx.ServerConfig.Add(
                new ServerConfigEntity
                {
                    Key = key,
                    Value = value,
                    Description = description,
                }
            );
        }
        else
        {
            entity.Value = value;

            if (description is not null)
            {
                entity.Description = description;
            }
        }

        await dbCtx.SaveChangesAsync().ConfigureAwait(true);

        _cache[key] = value;
    }

    public Task<ImmutableDictionary<string, string>> GetAllAsync() =>
        Task.FromResult(_cache.ToImmutableDictionary());

    public Task ReloadAsync() => LoadAsync(CancellationToken.None);

    public Task<ImmutableArray<string>> GetMotdAsync()
    {
        if (!_cache.TryGetValue(MotdKey, out string? json) || string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(ImmutableArray<string>.Empty);
        }

        try
        {
            string[]? lines = JsonSerializer.Deserialize<string[]>(json);

            return Task.FromResult(lines is null ? ImmutableArray<string>.Empty : [.. lines]);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Malformed MOTD config value under key {Key}; ignoring",
                MotdKey
            );

            return Task.FromResult(ImmutableArray<string>.Empty);
        }
    }

    public Task SetMotdAsync(ImmutableArray<string> lines) =>
        SetValueAsync(
            MotdKey,
            JsonSerializer.Serialize(lines),
            "Message-of-the-day lines (JSON array)"
        );

    private async Task LoadAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<ServerConfigEntity> rows = await dbCtx
            .ServerConfig.AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(true);

        _cache.Clear();

        foreach (ServerConfigEntity row in rows)
        {
            _cache[row.Key] = row.Value;
        }

        _logger.LogInformation("Loaded server config: Count={Count}", _cache.Count);
    }
}
