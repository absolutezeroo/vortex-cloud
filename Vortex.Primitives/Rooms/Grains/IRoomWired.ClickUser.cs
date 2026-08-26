using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Primitives.Rooms.Grains;

public partial interface IRoomWired
{
    /// <summary>
    /// Whether the room has a click-user wired box, and whether it suppresses the context menu.
    /// </summary>
    /// <remarks>
    /// Read on room entry to tell the client whether to route avatar clicks through wired at all,
    /// and again per click to answer it. Deliberately a query: the click event itself is published
    /// by <c>ClickCharacter</c> and publishing it here as well would fire every click-user trigger
    /// twice for one click.
    /// </remarks>
    public Task<WiredClickUserSnapshot> GetClickUserStateAsync(CancellationToken ct);
}
