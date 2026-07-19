using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Catalog.Grains;

public sealed class VoucherGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<VoucherGrain> logger
) : Grain, IVoucherGrain
{
    private VoucherEntity? _voucher;

    private string Code => this.GetPrimaryKeyString();

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        _voucher = await dbCtx
            .Vouchers.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Code == Code && v.DeletedAt == null, ct);
    }

    public async Task<VoucherCreateResult> CreateAsync(VoucherCreateSpec spec, CancellationToken ct)
    {
        if (_voucher is not null)
        {
            return FailCreate("code_already_exists");
        }

        if (spec.Amount <= 0)
        {
            return FailCreate("invalid_amount");
        }

        if (spec.CurrencyType == CurrencyType.ActivityPoints && spec.ActivityPointType is null)
        {
            return FailCreate("activity_point_type_required");
        }

        if (spec.MaxRedemptions is <= 0)
        {
            return FailCreate("invalid_max_redemptions");
        }

        VoucherEntity entity = new()
        {
            Code = Code,
            CurrencyType = spec.CurrencyType,
            ActivityPointType = spec.ActivityPointType,
            Amount = spec.Amount,
            MaxRedemptions = spec.MaxRedemptions,
            IsActive = true,
            ExpiresAt = spec.ExpiresAt,
            CreatedBy = spec.CreatedBy,
        };

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        dbCtx.Vouchers.Add(entity);
        await dbCtx.SaveChangesAsync(ct);

        _voucher = entity;

        logger.LogInformation(
            "Voucher {Code} created by {CreatedBy} ({CurrencyType} x{Amount})",
            Code,
            spec.CreatedBy,
            spec.CurrencyType,
            spec.Amount
        );

        return new VoucherCreateResult { Success = true, ErrorCode = string.Empty };
    }

    public async Task<VoucherRedeemResult> RedeemAsync(PlayerId playerId, CancellationToken ct)
    {
        VoucherRedeemResult result = await TryRedeemAsync(playerId, ct).ConfigureAwait(true);

        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(playerId);

        await presence
            .SendComposerAsync(
                result.Success
                    ? new VoucherRedeemOkMessageComposer()
                    : new VoucherRedeemErrorMessageComposer()
            )
            .ConfigureAwait(true);

        return result;
    }

    private async Task<VoucherRedeemResult> TryRedeemAsync(PlayerId playerId, CancellationToken ct)
    {
        if (_voucher is null)
        {
            return FailRedeem("not_found");
        }

        if (!_voucher.IsActive)
        {
            return FailRedeem("inactive");
        }

        if (_voucher.ExpiresAt is not null && _voucher.ExpiresAt.Value < DateTime.UtcNow)
        {
            return FailRedeem("expired");
        }

        int playerIdInt = playerId.Value;

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        bool alreadyRedeemed = await dbCtx.VoucherRedemptions.AnyAsync(
            v => v.VoucherEntityId == _voucher.Id && v.PlayerEntityId == playerIdInt,
            ct
        );

        if (alreadyRedeemed)
        {
            return FailRedeem("already_redeemed");
        }

        if (_voucher.MaxRedemptions is not null)
        {
            int redemptionCount = await dbCtx.VoucherRedemptions.CountAsync(
                v => v.VoucherEntityId == _voucher.Id,
                ct
            );

            if (redemptionCount >= _voucher.MaxRedemptions.Value)
            {
                return FailRedeem("max_redemptions_reached");
            }
        }

        PlayerEntity? playerEntity = await dbCtx.Players.FindAsync([playerIdInt], ct);

        if (playerEntity is null)
        {
            return FailRedeem("player_not_found");
        }

        dbCtx.VoucherRedemptions.Add(
            new VoucherRedemptionEntity
            {
                VoucherEntityId = _voucher.Id,
                PlayerEntityId = playerIdInt,
                RedeemedAt = DateTime.UtcNow,
                PlayerEntity = playerEntity,
            }
        );

        await dbCtx.SaveChangesAsync(ct);

        IPlayerWalletGrain wallet = grainFactory.GetPlayerWalletGrain(playerId);

        if (_voucher.CurrencyType == CurrencyType.ActivityPoints)
        {
            await wallet
                .GrantActivityPointsAsync(_voucher.ActivityPointType!.Value, _voucher.Amount, ct)
                .ConfigureAwait(true);
        }
        else
        {
            await wallet.GrantCreditsAsync(_voucher.Amount, ct).ConfigureAwait(true);
        }

        logger.LogInformation("Voucher {Code} redeemed by player {PlayerId}", Code, playerIdInt);

        return new VoucherRedeemResult { Success = true, ErrorCode = string.Empty };
    }

    public async Task<VoucherCreateResult> DeactivateAsync(CancellationToken ct)
    {
        if (_voucher is null)
        {
            return FailCreate("not_found");
        }

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        VoucherEntity? entity = await dbCtx.Vouchers.FindAsync([_voucher.Id], ct);

        if (entity is null)
        {
            return FailCreate("not_found");
        }

        entity.IsActive = false;
        await dbCtx.SaveChangesAsync(ct);

        _voucher.IsActive = false;

        return new VoucherCreateResult { Success = true, ErrorCode = string.Empty };
    }

    public async Task<VoucherSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        if (_voucher is null)
        {
            return new VoucherSnapshot
            {
                Exists = false,
                Code = Code,
                IsActive = false,
                CurrencyType = CurrencyType.Credits,
                Amount = 0,
                RedemptionCount = 0,
            };
        }

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        int redemptionCount = await dbCtx.VoucherRedemptions.CountAsync(
            v => v.VoucherEntityId == _voucher.Id,
            ct
        );

        return new VoucherSnapshot
        {
            Exists = true,
            Code = _voucher.Code,
            IsActive = _voucher.IsActive,
            CurrencyType = _voucher.CurrencyType,
            ActivityPointType = _voucher.ActivityPointType,
            Amount = _voucher.Amount,
            MaxRedemptions = _voucher.MaxRedemptions,
            RedemptionCount = redemptionCount,
            ExpiresAt = _voucher.ExpiresAt,
        };
    }

    private static VoucherCreateResult FailCreate(string errorCode) =>
        new() { Success = false, ErrorCode = errorCode };

    private static VoucherRedeemResult FailRedeem(string errorCode) =>
        new() { Success = false, ErrorCode = errorCode };
}
