using System.Collections.Generic;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Grains.Storage;

public sealed class FurnitureActiveStore : ActiveStore
{
    private readonly Dictionary<RoomObjectId, KeyValueStore> _byItemId = [];

    public bool RemoveFurnitureStore(RoomObjectId objectId) => _byItemId.Remove(objectId);

    public override bool TryGetStore(WiredVariableKey key, out KeyValueStore? store)
    {
        store = null;

        if (key.TargetType != WiredVariableTargetType.Furni)
        {
            return false;
        }

        RoomObjectId targetId = RoomObjectId.Parse(key.TargetId);

        if (!_byItemId.TryGetValue(targetId, out KeyValueStore? found))
        {
            found = new KeyValueStore();

            _byItemId[targetId] = found;
        }

        store = found;

        return true;
    }
}
