using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Prizes.Snapshots;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Crackable furniture: several hits, then a prize. Lives on the room grain because the counters are
/// per instance and shared by everyone in the room — the grain's single turn is what stops two
/// simultaneous clicks from both landing the final hit and paying out twice.
/// </summary>
public sealed class RoomCrackableSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    public async Task HitCrackableAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(objectId, out IRoomItem? item))
        {
            return;
        }

        if (item.Logic.StuffData is not ICrackableStuffData crackable)
        {
            // The definition runs the crackable logic but its stuff data was created under another
            // format, so the counters have nowhere to live. Silently doing nothing on every click
            // would look like a dead furniture with no explanation.
            _roomGrain._logger.LogWarning(
                "Crackable {ObjectId} (definition {DefinitionId}) in room {RoomId} does not carry crackable stuff data; its furniture definition needs stuff data type {ExpectedType}.",
                objectId.Value,
                item.Definition.Id,
                _roomGrain._state.RoomId.Value,
                (int)StuffDataType.CrackableKey
            );

            return;
        }

        PrizeBindingSnapshot? binding = await _roomGrain
            ._grainFactory.GetPrizePoolManagerGrain()
            .GetBindingAsync(item.Definition.Id, ct)
            .ConfigureAwait(true);

        if (binding is null)
        {
            _roomGrain._logger.LogWarning(
                "Crackable {ObjectId} (definition {DefinitionId}) in room {RoomId} is bound to no prize pool, so it can never pay out.",
                objectId.Value,
                item.Definition.Id,
                _roomGrain._state.RoomId.Value
            );

            return;
        }

        // The target travels with the binding rather than the instance, so retuning an event mid-run
        // applies to boxes already placed instead of only to the ones handed out afterwards.
        crackable.SetTarget(binding.HitsRequired);

        int hits = crackable.AddHit();

        // The state doubles as the damage stage: assets that ship one per hit animate for free, and
        // one that does not simply stays put. This also persists the counters and pushes the update.
        await item.Logic.SetStateAsync(hits).ConfigureAwait(true);

        if (hits < binding.HitsRequired)
        {
            return;
        }

        // Consume first: if the grant then fails the room is out one crackable, but the reverse order
        // would let a repeated click on a furniture that is still there mint prizes.
        if (!await ConsumeAsync(ctx, item, ct).ConfigureAwait(true))
        {
            return;
        }

        PrizeEntrySnapshot? prize = await _roomGrain
            ._grainFactory.GetPrizePoolManagerGrain()
            .PickAsync(binding.PoolCode, string.Empty, ct)
            .ConfigureAwait(true);

        if (prize is null)
        {
            _roomGrain._logger.LogWarning(
                "Crackable {ObjectId} in room {RoomId} was cracked but pool '{PoolCode}' had nothing to award to player {PlayerId}.",
                objectId.Value,
                _roomGrain._state.RoomId.Value,
                binding.PoolCode,
                ctx.PlayerId.Value
            );

            return;
        }

        // The prize goes to whoever landed the final hit, which is what the room watched happen.
        await _roomGrain
            ._grainFactory.GetPlayerPrizeGrain(ctx.PlayerId)
            .GrantAsync(prize, PrizeSources.Crackable, ct)
            .ConfigureAwait(true);
    }

    public async Task ClaimWelcomeGiftAsync(
        ActionContext ctx,
        RoomObjectId objectId,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(objectId, out IRoomItem? item))
        {
            return;
        }

        PrizeBindingSnapshot? binding = await _roomGrain
            ._grainFactory.GetPrizePoolManagerGrain()
            .GetBindingAsync(item.Definition.Id, ct)
            .ConfigureAwait(true);

        if (binding is null)
        {
            _roomGrain._logger.LogWarning(
                "Welcome gift {ObjectId} (definition {DefinitionId}) in room {RoomId} is bound to no prize pool, so it can never pay out.",
                objectId.Value,
                item.Definition.Id,
                _roomGrain._state.RoomId.Value
            );

            return;
        }

        PrizeEntrySnapshot? prize = await _roomGrain
            ._grainFactory.GetPrizePoolManagerGrain()
            .PickAsync(binding.PoolCode, string.Empty, ct)
            .ConfigureAwait(true);

        if (prize is null)
        {
            return;
        }

        // Nothing is consumed here: the gift stays for the next player, and the grain's
        // once-per-player claim is what stops this one taking it again. A null award means they
        // already have -- a quiet no-op, which is what the client expects from a second click.
        await _roomGrain
            ._grainFactory.GetPlayerPrizeGrain(ctx.PlayerId)
            .GrantOnceAsync(prize, binding.PoolId, PrizeSources.WelcomeGift, ct)
            .ConfigureAwait(true);
    }

    /// <summary>Removes the cracked furniture from the room and the database. Returns false when the
    /// delete did not happen, so the caller can avoid handing out a prize for it.</summary>
    private async Task<bool> ConsumeAsync(ActionContext ctx, IRoomItem item, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _roomGrain
                ._dbCtxFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FurnitureEntity entity = new() { Id = item.ObjectId.Value };
            dbCtx.Attach(entity);
            entity.DeletedAt = DateTime.UtcNow;
            dbCtx.Entry(entity).Property(f => f.DeletedAt).IsModified = true;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogError(
                ex,
                "Failed to delete cracked furniture {ObjectId} in room {RoomId}",
                item.ObjectId.Value,
                _roomGrain._state.RoomId.Value
            );

            return false;
        }

        await _roomGrain
            .ObjectModule.RemoveObjectAsync(ctx, item, ct, item.OwnerId)
            .ConfigureAwait(true);

        return true;
    }
}
