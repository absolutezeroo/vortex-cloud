using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Collectibles.Grains;

/// <summary>
/// One player's whole-avatar wardrobe: what they own, and what they are wearing.
/// </summary>
internal sealed class PlayerNftWardrobeGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<PlayerNftWardrobeGrain> logger
) : Grain, IPlayerNftWardrobeGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly IEventPublisher _events = events;
    private readonly ILogger<PlayerNftWardrobeGrain> _logger = logger;

    private PlayerId PlayerId => new((int)this.GetPrimaryKeyLong());

    /// <summary>
    /// The token that stands for one copy. Built rather than stored, so the one in the wardrobe list
    /// and the one in the worn-outfit answer cannot drift apart — the client matches them against
    /// each other, and a mismatch means the editor cannot find the tile it should light up.
    /// </summary>
    private static string TokenOf(string contractKey, int copyId) =>
        string.Create(CultureInfo.InvariantCulture, $"{contractKey}:{copyId}");

    public async Task<ImmutableArray<NftAvatarSnapshot>> GetWardrobeAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        var copies = await dbCtx
            .PlayerNftAvatars.AsNoTracking()
            .Where(owned =>
                owned.PlayerEntityId == PlayerId.Value
                && owned.DeletedAt == null
                && owned.NftAvatarEntity != null
                && owned.NftAvatarEntity.Enabled
                && owned.NftAvatarEntity.DeletedAt == null
            )
            .OrderBy(owned => owned.NftAvatarEntity!.SortOrder)
            .ThenBy(owned => owned.Id)
            .Select(owned => new
            {
                owned.Id,
                owned.NftAvatarEntity!.Figure,
                owned.NftAvatarEntity.Gender,
                owned.NftAvatarEntity.ContractKey,
            })
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        return
        [
            .. copies.Select(copy => new NftAvatarSnapshot
            {
                CopyId = copy.Id,
                Figure = copy.Figure,
                Gender = copy.Gender,
                TokenId = TokenOf(copy.ContractKey, copy.Id),
                ContractKey = copy.ContractKey,
            }),
        ];
    }

    public async Task<NftOutfitSnapshot?> GetWornAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        return await ReadWornAsync(dbCtx, ct).ConfigureAwait(true);
    }

    public async Task<NftOutfitSnapshot?> WearAsync(int copyId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        NftAvatarEntity? avatar = await dbCtx
            .PlayerNftAvatars.AsNoTracking()
            .Where(owned =>
                owned.Id == copyId
                && owned.PlayerEntityId == PlayerId.Value
                && owned.DeletedAt == null
                && owned.NftAvatarEntity != null
                && owned.NftAvatarEntity.Enabled
                && owned.NftAvatarEntity.DeletedAt == null
            )
            .Select(owned => owned.NftAvatarEntity!)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(true);

        if (avatar is null)
        {
            // Holding the copy is the whole permission model -- there is no chain to ask, and the
            // client is only ever offered what it was sent, so anything else did not come from the
            // editor.
            _logger.LogWarning(
                "Player {PlayerId} tried to wear avatar copy {CopyId}, which is not theirs.",
                PlayerId,
                copyId
            );

            return null;
        }

        PlayerSummarySnapshot snapshot = await _grainFactory
            .GetPlayerGrain(PlayerId)
            .GetSummaryAsync(ct)
            .ConfigureAwait(true);

        PlayerNftOutfitEntity? worn = await dbCtx
            .PlayerNftOutfits.FirstOrDefaultAsync(
                row => row.PlayerEntityId == PlayerId.Value && row.DeletedAt == null,
                ct
            )
            .ConfigureAwait(true);

        // The fallback is the look they had *before the first* costume, not before this one:
        // swapping avatar for avatar must not make the previous costume the way home.
        if (worn is null)
        {
            dbCtx.PlayerNftOutfits.Add(
                new PlayerNftOutfitEntity
                {
                    PlayerEntityId = PlayerId.Value,
                    PlayerNftAvatarEntityId = copyId,
                    FallbackFigure = snapshot.Figure,
                    FallbackGender = snapshot.Gender.ToLegacyString(),
                }
            );
        }
        else
        {
            worn.PlayerNftAvatarEntityId = copyId;
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        await _grainFactory
            .GetPlayerGrain(PlayerId)
            .SetFigureAsync(
                avatar.Figure,
                AvatarGenderTypeExtensions.FromLegacyString(avatar.Gender),
                ct
            )
            .ConfigureAwait(true);

        _logger.LogInformation(
            "Player {PlayerId} is wearing avatar copy {CopyId} ({AvatarCode}).",
            PlayerId,
            copyId,
            avatar.AvatarCode
        );

        await _events
            .PublishAsync(new NftAvatarWornEvent(PlayerId, copyId), ct)
            .ConfigureAwait(true);

        return await ReadWornAsync(dbCtx, ct).ConfigureAwait(true);
    }

    public async Task RemoveWornAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        // The figure is not restored here. This runs when the player has just saved a look of their
        // own choosing, and that look is the one they want -- putting the fallback back would undo
        // the change they made.
        await dbCtx
            .PlayerNftOutfits.Where(row =>
                row.PlayerEntityId == PlayerId.Value && row.DeletedAt == null
            )
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(true);

        await _events.PublishAsync(new NftAvatarWornEvent(PlayerId, null), ct).ConfigureAwait(true);
    }

    private async Task<NftOutfitSnapshot?> ReadWornAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        var worn = await dbCtx
            .PlayerNftOutfits.AsNoTracking()
            .Where(row =>
                row.PlayerEntityId == PlayerId.Value
                && row.DeletedAt == null
                && row.PlayerNftAvatarEntity != null
                && row.PlayerNftAvatarEntity.NftAvatarEntity != null
            )
            .Select(row => new
            {
                CopyId = row.PlayerNftAvatarEntityId,
                row.PlayerNftAvatarEntity!.NftAvatarEntity!.ContractKey,
                row.FallbackFigure,
                row.FallbackGender,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(true);

        return worn is null
            ? null
            : new NftOutfitSnapshot
            {
                TokenId = TokenOf(worn.ContractKey, worn.CopyId),
                FallbackFigure = worn.FallbackFigure,
                FallbackGender = worn.FallbackGender,
            };
    }
}
