using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Groups.Snapshots;

namespace Vortex.Primitives.Groups.Providers;

public interface IGroupBadgePartProvider
{
    IReadOnlyList<GroupBadgePartOptionSnapshot> BaseParts { get; }

    IReadOnlyList<GroupBadgePartOptionSnapshot> LayerParts { get; }

    IReadOnlyList<GroupColorOptionSnapshot> Colors { get; }

    string ResolveColorHex(string? colorId);

    Task ReloadAsync(CancellationToken ct);
}
