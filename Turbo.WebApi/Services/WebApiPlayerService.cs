using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Enums;
using Turbo.WebApi.Configuration;

namespace Turbo.WebApi.Services;

public sealed class WebApiPlayerService(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    IOptions<WebApiConfig> options,
    ILogger<WebApiPlayerService> logger
) : IWebApiPlayerService
{
    private readonly IDbContextFactory<TurboDbContext> _db = dbCtxFactory;
    private readonly WebApiConfig _config = options.Value;
    private readonly ILogger<WebApiPlayerService> _logger = logger;

    public async Task<List<AvatarInfo>> GetAvatarsForAccountAsync(
        int accountId,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<PlayerEntity> players = await db
            .Players.AsNoTracking()
            .Where(p => p.PlayerAccountEntityId == accountId && p.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return players.Select(ToAvatarInfo).ToList();
    }

    public async Task<(bool Success, int PlayerId, string? Error)> CreateAvatarAsync(
        int accountId,
        string name,
        string figure,
        string gender,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        int count = await db
            .Players.AsNoTracking()
            .CountAsync(p => p.PlayerAccountEntityId == accountId && p.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (count >= _config.MaxAvatarsPerAccount)
        {
            _logger.LogWarning(
                "Avatar creation refused: account {AccountId} reached max avatars ({Max})",
                accountId,
                _config.MaxAvatarsPerAccount
            );
            return (false, 0, "pocket.auth.max_avatars_reached");
        }

        bool taken = await db
            .Players.AsNoTracking()
            .AnyAsync(p => p.Name == name, ct)
            .ConfigureAwait(false);

        if (taken)
        {
            _logger.LogWarning("Avatar creation refused: name '{Name}' already taken", name);
            return (false, 0, "pocket.auth.name_taken");
        }

        AvatarGenderType genderType = AvatarGenderTypeExtensions.FromLegacyString(gender);

        PlayerEntity player = new PlayerEntity
        {
            PlayerAccountEntityId = accountId,
            Name = name,
            Figure = string.IsNullOrWhiteSpace(figure) ? _config.DefaultFigure : figure,
            Gender = genderType,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };

        db.Players.Add(player);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Avatar '{Name}' created for account {AccountId} (playerId={PlayerId})",
            name,
            accountId,
            player.Id
        );
        return (true, player.Id, null);
    }

    public async Task<bool> NameAvailableAsync(string name, CancellationToken ct)
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);
        return !await db.Players.AnyAsync(p => p.Name == name, ct).ConfigureAwait(false);
    }

    public async Task<bool> SetNameAsync(int playerId, string name, CancellationToken ct)
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        bool taken = await db
            .Players.AsNoTracking()
            .AnyAsync(p => p.Name == name && p.Id != playerId, ct)
            .ConfigureAwait(false);

        if (taken)
        {
            return false;
        }

        PlayerEntity? player = await db
            .Players.FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            return false;
        }

        player.Name = name;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SaveFigureAsync(
        int playerId,
        string figureString,
        string gender,
        CancellationToken ct
    )
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        if (player is null)
        {
            return false;
        }

        player.Figure = figureString;
        player.Gender = AvatarGenderTypeExtensions.FromLegacyString(gender);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<AvatarInfo?> GetAvatarAsync(int playerId, CancellationToken ct)
    {
        await using TurboDbContext db = await _db.CreateDbContextAsync(ct).ConfigureAwait(false);

        PlayerEntity? player = await db
            .Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId, ct)
            .ConfigureAwait(false);

        return player is null ? null : ToAvatarInfo(player);
    }

    private static AvatarInfo ToAvatarInfo(PlayerEntity p) =>
        new(
            UniqueId: p.Id.ToString(),
            Name: p.Name,
            Motto: p.Motto ?? string.Empty,
            FigureString: p.Figure,
            Gender: p.Gender.ToLegacyString()
        );
}
