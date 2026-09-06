using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Action;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Dashboard.API.Operations;

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
