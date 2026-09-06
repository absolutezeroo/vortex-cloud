using System;
using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Create a redeemable voucher code. <paramref name="CurrencyType"/> is 1=Credits, 2=Silver,
/// 3=Emeralds, 4=ActivityPoints (see <c>Vortex.Primitives.Players.Enums.Wallet.CurrencyType</c>);
/// <paramref name="ActivityPointType"/> is required only when <paramref name="CurrencyType"/> is
/// ActivityPoints. <paramref name="MaxRedemptions"/> null means unlimited (across different
/// players — each player may still redeem a given code only once).
/// </summary>
public sealed record CreateVoucherRequest(
    string Code,
    int CurrencyType,
    int? ActivityPointType,
    int Amount,
    int? MaxRedemptions,
    DateTime? ExpiresAt,
    string Reason
) : IReasonedRequest;
