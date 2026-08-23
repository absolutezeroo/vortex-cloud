using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Wired;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Outgoing.Userdefinedroomevents.Wiredtrading;

namespace Vortex.Rooms.Grains;

/// <summary>
/// Where a contract's terms are written down.
/// </summary>
/// <remarks>
/// A contract furni has an editor of its own — not the wired dialog — and this is what it reads and
/// saves. The terms are a tree, so they are stored as JSON in one column: nothing queries inside
/// them and the only reader wants the whole thing at once.
/// <para>
/// Writing one is a decorating right, not an ownership one: whoever may lay out the room may price
/// what stands in it. Reading is the same right, because the editor is the only thing that asks.
/// </para>
/// </remarks>
public sealed partial class RoomGrain
{
    /// <summary>The furni this hotel calls a contract.</summary>
    private const string ContractLogicPrefix = "wf_contract_";

    private static readonly JsonSerializerOptions ContractJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>The stored shape. Deliberately its own type: the wire records may move, and a
    /// column written last week has to keep reading.</summary>
    private sealed record StoredDefinition(List<StoredRule>? YouGiveRules, StoredRule? YouGetRule);

    private sealed record StoredRule(List<StoredNode> Nodes);

    private sealed record StoredNode(
        bool IsFurni,
        int Amount,
        bool IsWallItem,
        int SpriteId,
        string LegacyPosterId
    );

    /// <summary>Whether this player may edit what stands in this room.</summary>
    private async Task<bool> CanEditContractAsync(ActionContext ctx, int contractId)
    {
        if (
            ctx.PlayerId <= 0
            || !_state.ItemsById.TryGetValue(contractId, out IRoomItem? item)
            || !item.Definition.LogicName.StartsWith(ContractLogicPrefix, StringComparison.Ordinal)
        )
        {
            return false;
        }

        RoomControllerType level = await SecurityModule
            .GetControllerLevelAsync(ctx)
            .ConfigureAwait(true);

        return level != RoomControllerType.None;
    }

    /// <summary>
    /// One contract's terms, for its editor.
    /// </summary>
    /// <remarks>
    /// A contract nobody has written yet answers with an empty one rather than with nothing: the
    /// editor has to open before it can be filled in, and its type comes from the furni itself.
    /// </remarks>
    public async Task<WiredContractSnapshot?> GetWiredContractAsync(
        ActionContext ctx,
        int contractId,
        CancellationToken ct
    )
    {
        if (!await CanEditContractAsync(ctx, contractId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredContractEntity? row = await dbCtx
                .WiredContracts.AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == contractId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            return ForEditor(row is null ? EmptyContract(contractId) : ToContractSnapshot(row));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to read contract {ContractId} in room {RoomId}.",
                contractId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>
    /// The editor's save, answered with what was actually stored.
    /// </summary>
    /// <remarks>
    /// The type comes back from the furni rather than from the message: the client sends the one it
    /// has an editor open for, and a contract that could be re-typed by a packet is a payment
    /// contract that turns into a reward one.
    /// </remarks>
    public async Task<WiredContractSnapshot?> SaveWiredContractAsync(
        ActionContext ctx,
        WiredContractSnapshot contract,
        CancellationToken ct
    )
    {
        if (!await CanEditContractAsync(ctx, contract.ContractId).ConfigureAwait(true))
        {
            return null;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredContractEntity? row = await dbCtx
                .WiredContracts.FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == contract.ContractId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                row = new WiredContractEntity
                {
                    FurnitureEntityId = contract.ContractId,
                    ContractType = ContractTypeOf(contract.ContractId),
                };

                dbCtx.WiredContracts.Add(row);
            }

            row.ContractType = ContractTypeOf(contract.ContractId);
            row.Definition = JsonSerializer.Serialize(
                new StoredDefinition(
                    contract.YouGiveRules is { } give
                        ? [.. System.Linq.Enumerable.Select(give, ToStoredRule)]
                        : null,
                    contract.YouGetRule is null ? null : ToStoredRule(contract.YouGetRule)
                ),
                ContractJson
            );
            row.PaymentMode = contract.PaymentMode;
            row.ReceiveText = contract.ReceiveText;
            row.LayoutType = contract.LayoutType;
            row.RewardCategory = contract.RewardCategory;
            row.ShowDialog = contract.ShowDialog;
            row.RewardText = contract.RewardText;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

            return ForEditor(ToContractSnapshot(row));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to save contract {ContractId} in room {RoomId}.",
                contract.ContractId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>
    /// The terms a contract furni carries, for the box that offers it.
    /// </summary>
    /// <remarks>
    /// Read without a permission check on purpose: this is not the editor, it is the offer, and the
    /// player being offered a contract is not the one who may edit it.
    /// </remarks>
    internal async Task<WiredContractSnapshot?> ReadStoredContractAsync(
        int contractId,
        CancellationToken ct
    )
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredContractEntity? row = await dbCtx
                .WiredContracts.AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.FurnitureEntityId == contractId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(true);

            return row is null ? null : ToContractSnapshot(row);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to read the terms of contract {ContractId} in room {RoomId}.",
                contractId,
                RoomId
            );

            return null;
        }
    }

    /// <summary>The furni's own three names, which is where a contract's type comes from.</summary>
    private int ContractTypeOf(int contractId) =>
        _state.ItemsById.TryGetValue(contractId, out IRoomItem? item)
            ? item.Definition.LogicName switch
            {
                "wf_contract_payment" => PaymentContractType,
                "wf_contract_trade" => TradeContractType,
                "wf_contract_reward" => RewardContractType,
                _ => PaymentContractType,
            }
            : PaymentContractType;

    private WiredContractSnapshot EmptyContract(int contractId) =>
        new() { ContractId = contractId, ContractType = ContractTypeOf(contractId) };

    /// <summary>
    /// The same contract, with the sides its editor refuses to open without.
    /// </summary>
    /// <remarks>
    /// Each editor drops the message on the floor when the side its type is about is missing: a
    /// payment contract needs the give side, a reward contract the receive side, a trade contract
    /// both. A contract nobody has written has neither, so sending it as stored opened nothing at
    /// all — the window never appeared and nothing said why.
    /// <para>
    /// An empty side and an absent one mean different things to the offer, where a give side that
    /// is not there asks for nothing. So this fills them for the editor only, and the offer keeps
    /// reading what is actually stored.
    /// </para>
    /// </remarks>
    private static WiredContractSnapshot ForEditor(WiredContractSnapshot contract)
    {
        bool needsGive =
            contract.ContractType == PaymentContractType
            || contract.ContractType == TradeContractType;

        bool needsGet =
            contract.ContractType == RewardContractType
            || contract.ContractType == TradeContractType;

        return contract with
        {
            YouGiveRules =
                contract.YouGiveRules
                ?? (needsGive ? ImmutableArray<TradeContractRule>.Empty : null),
            YouGetRule =
                contract.YouGetRule ?? (needsGet ? new TradeContractRule { Nodes = [] } : null),
        };
    }

    private const int PaymentContractType = 0;

    private const int TradeContractType = 1;

    private const int RewardContractType = 2;

    private WiredContractSnapshot ToContractSnapshot(WiredContractEntity row)
    {
        StoredDefinition? definition = null;

        if (!string.IsNullOrEmpty(row.Definition))
        {
            try
            {
                definition = JsonSerializer.Deserialize<StoredDefinition>(
                    row.Definition,
                    ContractJson
                );
            }
            catch (JsonException ex)
            {
                // Unreadable terms are no terms. Saying so beats offering a contract that asks for
                // whatever survived the parse.
                _logger.LogWarning(
                    ex,
                    "Contract {ContractId} in room {RoomId} has terms that cannot be read.",
                    row.FurnitureEntityId,
                    RoomId
                );
            }
        }

        return new WiredContractSnapshot
        {
            ContractId = row.FurnitureEntityId,
            ContractType = row.ContractType,
            YouGiveRules = definition?.YouGiveRules is { } give
                ? [.. System.Linq.Enumerable.Select(give, FromStoredRule)]
                : null,
            YouGetRule = definition?.YouGetRule is null
                ? null
                : FromStoredRule(definition.YouGetRule),
            PaymentMode = row.PaymentMode,
            ReceiveText = row.ReceiveText,
            LayoutType = row.LayoutType,
            RewardCategory = row.RewardCategory,
            ShowDialog = row.ShowDialog,
            RewardText = row.RewardText,
        };
    }

    private static StoredRule ToStoredRule(TradeContractRule rule) =>
        new([
            .. System.Linq.Enumerable.Select(
                rule.Nodes,
                node => new StoredNode(
                    node.IsFurni,
                    node.Amount,
                    node.ItemType?.IsWallItem ?? false,
                    node.ItemType?.SpriteId ?? 0,
                    node.ItemType?.LegacyPosterId ?? string.Empty
                )
            ),
        ]);

    private static TradeContractRule FromStoredRule(StoredRule rule) =>
        new()
        {
            Nodes =
            [
                .. System.Linq.Enumerable.Select(
                    rule.Nodes,
                    node => new TradeContractNode
                    {
                        IsFurni = node.IsFurni,
                        Amount = node.Amount,
                        ItemType = node.IsFurni
                            ? new TradeContractItemType
                            {
                                IsWallItem = node.IsWallItem,
                                SpriteId = node.SpriteId,
                                LegacyPosterId = node.LegacyPosterId,
                            }
                            : null,
                    }
                ),
            ],
        };

    /// <summary>
    /// The chest upgrade dialog's buy button.
    /// </summary>
    /// <remarks>
    /// What each upgrade costs and gives is a catalogue question this hotel has no answer for yet —
    /// the client sends only a number, and nothing in it says what that number buys. So the request
    /// is refused rather than guessed at: a capacity granted for free is worse than one that has to
    /// wait, and the column it would write is the one the settings screen is already forbidden from
    /// setting.
    /// </remarks>
    public Task<bool> UpgradeWiredChestAsync(
        ActionContext ctx,
        int chestId,
        int upgradeType,
        CancellationToken ct
    )
    {
        _logger.LogWarning(
            "Player {PlayerId} asked to buy chest upgrade {UpgradeType} for chest {ChestId} in room "
                + "{RoomId}; upgrades have no prices here, so nothing was granted.",
            (int)ctx.PlayerId,
            upgradeType,
            chestId,
            RoomId
        );

        return Task.FromResult(false);
    }
}
