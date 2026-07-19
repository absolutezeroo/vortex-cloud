using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Inventory.Badges;
using Vortex.Primitives.Messages.Outgoing.Inventory.Furni;
using Vortex.Primitives.Messages.Outgoing.Inventory.Pets;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Players.Grains.Modules;

internal sealed class PlayerInventoryModule(PlayerPresenceGrain presenceGrain)
{
    private readonly PlayerPresenceGrain _presenceGrain = presenceGrain;

    public async Task OpenFurnitureInventoryAsync(CancellationToken ct)
    {
        IInventoryGrain inventoryGrain = _presenceGrain._grainFactory.GetInventoryGrain(
            _presenceGrain.GetPrimaryKeyLong()
        );
        ImmutableArray<FurnitureItemSnapshot> items = await inventoryGrain.GetAllItemSnapshotsAsync(
            ct
        );
        int furniPerFragment = _presenceGrain._config.FurniInventoryFragmentSize;

        int totalFragments = (int)
            Math.Max(1, Math.Ceiling((double)items.Length / furniPerFragment));
        int currentFragment = 0;
        int count = 0;
        List<FurnitureItemSnapshot> fragmentItems = [];

        foreach (FurnitureItemSnapshot item in items)
        {
            fragmentItems.Add(item);

            count++;

            if (count == furniPerFragment)
            {
                await _presenceGrain.SendComposerAsync(
                    new FurniListEventMessageComposer
                    {
                        TotalFragments = totalFragments,
                        CurrentFragment = currentFragment,
                        Items = [.. fragmentItems],
                    }
                );

                fragmentItems.Clear();
                count = 0;
                currentFragment++;
            }
        }

        await _presenceGrain.SendComposerAsync(
            new FurniListEventMessageComposer
            {
                TotalFragments = totalFragments,
                CurrentFragment = currentFragment,
                Items = [.. fragmentItems],
            }
        );
    }

    public Task OnFurnitureAddedAsync(FurnitureItemSnapshot snapshot, CancellationToken ct) =>
        _presenceGrain.SendComposerAsync(
            new FurniListAddOrUpdateEventMessageComposer { Item = snapshot }
        );

    public Task OnFurnitureRemovedAsync(RoomObjectId itemId, CancellationToken ct) =>
        _presenceGrain.SendComposerAsync(
            new FurniListRemoveEventMessageComposer { ItemId = itemId }
        );

    public Task OnBadgeGrantedAsync(string badgeCode, CancellationToken ct) =>
        _presenceGrain.SendComposerAsync(
            new BadgeReceivedEventMessageComposer { SlotId = 0, BadgeCode = badgeCode }
        );

    public Task OnPetAddedToInventoryAsync(PetSnapshot pet, CancellationToken ct) =>
        _presenceGrain.SendComposerAsync(new PetAddedToInventoryEventMessageComposer { Pet = pet });
}
