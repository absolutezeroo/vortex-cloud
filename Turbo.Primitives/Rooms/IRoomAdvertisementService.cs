using System;
using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Rooms;

public interface IRoomAdvertisementService
{
    Task CreateAsync(
        int roomId,
        string name,
        string? description,
        int categoryId,
        bool extended,
        DateTime expiresAt,
        CancellationToken ct = default
    );
}
