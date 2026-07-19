using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Snapshots.FriendList;

namespace Vortex.Primitives.Players.Grains;

public interface IPlayerDirectoryGrain : IGrainWithStringKey
{
    public Task<string> GetPlayerNameAsync(PlayerId playerId, CancellationToken ct);
    public Task<ImmutableDictionary<PlayerId, string>> GetPlayerNamesAsync(
        List<PlayerId> playerIds,
        CancellationToken ct
    );
    public Task<PlayerId?> GetPlayerIdAsync(string userName, CancellationToken ct);
    public Task SetPlayerNameAsync(PlayerId playerId, string name, CancellationToken ct);
    public Task<List<MessengerSearchResultSnapshot>> SearchPlayersAsync(
        string query,
        int limit,
        CancellationToken ct
    );
}
