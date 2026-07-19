using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Groups.Providers;
using Vortex.Primitives.Groups.Snapshots;

namespace Vortex.Rooms.Tests.Support;

internal sealed class NullGroupBadgePartProvider : IGroupBadgePartProvider
{
    public IReadOnlyList<GroupBadgePartOptionSnapshot> BaseParts => [];
    public IReadOnlyList<GroupBadgePartOptionSnapshot> LayerParts => [];
    public IReadOnlyList<GroupColorOptionSnapshot> Colors => [];

    public string ResolveColorHex(string? colorId) => string.Empty;

    public Task ReloadAsync(CancellationToken ct) => Task.CompletedTask;
}
