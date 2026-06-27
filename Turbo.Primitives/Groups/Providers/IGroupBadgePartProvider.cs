using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Groups.Snapshots;

namespace Turbo.Primitives.Groups.Providers;

public interface IGroupBadgePartProvider
{
    IReadOnlyList<GroupBadgePartOptionSnapshot> BaseParts { get; }

    IReadOnlyList<GroupBadgePartOptionSnapshot> LayerParts { get; }

    IReadOnlyList<GroupColorOptionSnapshot> Colors { get; }

    string ResolveColorHex(string? colorId);

    Task ReloadAsync(CancellationToken ct);
}
