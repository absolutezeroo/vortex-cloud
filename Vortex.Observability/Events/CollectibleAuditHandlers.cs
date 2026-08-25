using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Events;

/// <summary>
/// Collectibles as durable audit. The Relic's own provenance already lives in
/// <c>nft_asset_ledger</c>, which answers "where did this asset come from"; these records answer the
/// other half -- what the account did, on the same timeline as everything else it did that day.
/// </summary>
public sealed class RelicMintedAuditHandler(IAuditSink audit) : IEventHandler<RelicMintedEvent>
{
    public ValueTask HandleAsync(RelicMintedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "collectible.minted",
                // A mint destroys furniture to create something that cannot be made again once the
                // edition is full; it is the least reversible act a player has.
                Severity = AuditSeverity.Notice,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Amount = e.StampCost,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        e.AssetId,
                        e.DefinitionId,
                        serial = e.SerialNumber,
                    }
                ),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class MintTokensPurchasedAuditHandler(IAuditSink audit)
    : IEventHandler<MintTokensPurchasedEvent>
{
    public ValueTask HandleAsync(MintTokensPurchasedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.collectible.stamps_purchased",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Quantity = e.Quantity,
                Amount = e.Cost,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class NftStorePurchasedAuditHandler(IAuditSink audit)
    : IEventHandler<NftStorePurchasedEvent>
{
    public ValueTask HandleAsync(NftStorePurchasedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.collectible.store_purchase",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Amount = e.Price,
                Data = JsonSerializer.Serialize(new { product = e.ProductCode }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class NftClaimsCollectedAuditHandler(IAuditSink audit)
    : IEventHandler<NftClaimsCollectedEvent>
{
    public ValueTask HandleAsync(NftClaimsCollectedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Item,
                Action = "collectible.claims_collected",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Quantity = e.Count,
            }
        );

        return ValueTask.CompletedTask;
    }
}

public sealed class VaultIncomeClaimedAuditHandler(IAuditSink audit)
    : IEventHandler<VaultIncomeClaimedEvent>
{
    public ValueTask HandleAsync(VaultIncomeClaimedEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Economy,
                Action = "economy.vault.income_claimed",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Quantity = e.Rewards,
                Data = JsonSerializer.Serialize(new { category = e.Category }),
            }
        );

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A whole-avatar Relic went on or came off. A null copy means it came off, which is a different
/// action rather than the same one with a missing field.
/// </summary>
public sealed class NftAvatarWornAuditHandler(IAuditSink audit) : IEventHandler<NftAvatarWornEvent>
{
    public ValueTask HandleAsync(NftAvatarWornEvent e, EventContext ctx, CancellationToken ct)
    {
        audit.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Social,
                Action = e.CopyId is null
                    ? "profile.nft_avatar_removed"
                    : "profile.nft_avatar_worn",
                Severity = AuditSeverity.Info,
                Result = AuditResult.Success,
                ActorPlayerId = e.PlayerId,
                Data = e.CopyId is null
                    ? null
                    : JsonSerializer.Serialize(new { copyId = e.CopyId }),
            }
        );

        return ValueTask.CompletedTask;
    }
}
