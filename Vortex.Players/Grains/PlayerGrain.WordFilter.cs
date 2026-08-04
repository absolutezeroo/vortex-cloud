using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerGrain
{
    /// <summary>
    /// The client's input is a free-text field with no length limit of its own, so the cap is here.
    /// </summary>
    private const int MaxWordFilterWordLength = 64;

    /// <summary>
    /// A filter longer than this is refused rather than trimmed: the dialog lists every word in one
    /// scrolling column, and the client rebuilds that column on each change.
    /// </summary>
    private const int MaxWordFilterWords = 200;

    public async Task<ImmutableArray<string>> GetWordFilterAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        string[] words = await dbCtx
            .PlayerWordFilters.AsNoTracking()
            .Where(f => f.PlayerEntityId == _state.PlayerId.Value)
            .OrderBy(f => f.Id)
            .Select(f => f.Word)
            .ToArrayAsync(ct);

        return [.. words];
    }

    /// <summary>
    /// Adds a word, and answers whether the filter changed. A word already on the list is not an
    /// error — the client refuses to send one it can already see, so reaching here means two
    /// sessions raced, and the caller reports the add so both lists converge.
    /// </summary>
    public async Task<bool> AddWordFilterAsync(string word, CancellationToken ct)
    {
        string normalized = NormalizeWordFilterWord(word);

        if (normalized.Length == 0)
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        bool exists = await dbCtx.PlayerWordFilters.AnyAsync(
            f => f.PlayerEntityId == _state.PlayerId.Value && f.Word == normalized,
            ct
        );

        if (exists)
        {
            return true;
        }

        int count = await dbCtx.PlayerWordFilters.CountAsync(
            f => f.PlayerEntityId == _state.PlayerId.Value,
            ct
        );

        if (count >= MaxWordFilterWords)
        {
            return false;
        }

        dbCtx.PlayerWordFilters.Add(
            new PlayerWordFilterEntity { PlayerEntityId = _state.PlayerId.Value, Word = normalized }
        );

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return true;
    }

    /// <summary>
    /// Removes a word, and answers whether the filter changed. Removing something already gone
    /// answers false, so the client is not told to drop a row it is not showing.
    /// </summary>
    public async Task<bool> RemoveWordFilterAsync(string word, CancellationToken ct)
    {
        string normalized = NormalizeWordFilterWord(word);

        if (normalized.Length == 0)
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

        PlayerWordFilterEntity? entity = await dbCtx.PlayerWordFilters.FirstOrDefaultAsync(
            f => f.PlayerEntityId == _state.PlayerId.Value && f.Word == normalized,
            ct
        );

        if (entity is null)
        {
            return false;
        }

        dbCtx.PlayerWordFilters.Remove(entity);

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        return true;
    }

    private static string NormalizeWordFilterWord(string word) =>
        string.IsNullOrWhiteSpace(word)
            ? string.Empty
            : word.Trim()[..Math.Min(word.Trim().Length, MaxWordFilterWordLength)];
}
