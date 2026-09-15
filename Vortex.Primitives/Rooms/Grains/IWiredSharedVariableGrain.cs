using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>
/// The values of one shared wired variable, owned by nobody's room.
/// </summary>
/// <remarks>
/// A shared variable is read and written from at least two rooms — the one its box stands in and
/// every room with a reference variable naming it — and neither of them can be the authority: either
/// may be unloaded while the other runs. Orleans guarantees one activation per key, so this is the
/// single writer, and the rooms keep read replicas they refresh from it.
/// <para>
/// The key is the variable id. It is a 64-bit unsigned value against Orleans' signed integer key,
/// so the cast is unchecked on both sides — the bits round-trip either way, and in practice the ids
/// a variable box builds never reach the top bit at all: the source type sits at bits 48-52 and the
/// hash below it, so the value stays under 2^53.
/// </para>
/// </remarks>
public interface IWiredSharedVariableGrain : IGrainWithIntegerKey
{
    /// <summary>Everything this variable holds, for a room warming its replica.</summary>
    Task<ImmutableArray<WiredSharedValueSnapshot>> GetAllAsync(CancellationToken ct);

    /// <summary>Creates the value, or replaces an existing one when asked. False when the key is
    /// already there and <paramref name="replace"/> is false — the same contract the in-memory
    /// stores answer with.</summary>
    Task<bool> GiveAsync(string storageKey, int value, bool replace, CancellationToken ct);

    /// <summary>Updates a value that already exists, and refuses to create one.</summary>
    Task<bool> SetAsync(string storageKey, int value, CancellationToken ct);

    Task<bool> RemoveAsync(string storageKey, CancellationToken ct);
}
