using System.Collections.Generic;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Grains.Storage;

public sealed class PlayerActiveStore : ActiveStore
{
    private readonly Dictionary<PlayerId, KeyValueStore> _byPlayerId = [];

    public bool RemovePlayerStore(PlayerId playerId) => _byPlayerId.Remove(playerId);

    public override bool TryGetStore(WiredVariableKey key, out KeyValueStore? store)
    {
        store = null;

        if (key.TargetType != WiredVariableTargetType.User)
        {
            return false;
        }

        PlayerId targetId = PlayerId.Parse(key.TargetId);

        if (!_byPlayerId.TryGetValue(targetId, out KeyValueStore? found))
        {
            found = new KeyValueStore();

            _byPlayerId[targetId] = found;
        }

        store = found;

        return true;
    }
}
