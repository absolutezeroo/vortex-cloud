using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Action;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Dashboard.API.Operations;

internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateVoucherAsync(
        CreateVoucherRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.vouchers.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Code,
                request.CurrencyType,
                request.ActivityPointType,
                request.Amount,
                request.MaxRedemptions,
                request.ExpiresAt,
            },
            work: async c =>
            {
                VoucherCreateResult result = await _grainFactory
                    .GetVoucherGrain(request.Code)
                    .CreateAsync(
                        new VoucherCreateSpec
                        {
                            CurrencyType = (CurrencyType)request.CurrencyType,
                            ActivityPointType = request.ActivityPointType,
                            Amount = request.Amount,
                            MaxRedemptions = request.MaxRedemptions,
                            ExpiresAt = request.ExpiresAt,
                            CreatedBy = actor,
                        },
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeactivateVoucherAsync(
        DeactivateVoucherRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.vouchers.deactivate",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Code },
            work: async c =>
            {
                VoucherCreateResult result = await _grainFactory
                    .GetVoucherGrain(request.Code)
                    .DeactivateAsync(c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<VoucherSnapshot> GetVoucherSnapshotAsync(string code, CancellationToken ct) =>
        _grainFactory.GetVoucherGrain(code).GetSnapshotAsync(ct);
}
