using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Pets.Providers;

public interface IPetCommandProvider
{
    IReadOnlyList<PetCommandEntry> GetCommandsForType(int petType);

    ImmutableArray<int> GetAllCommandIds(int petType);

    ImmutableArray<int> GetEnabledCommandIds(int petType, int petLevel);

    PetCommandEntry? GetCommandConfig(int petType, int commandId);

    /// <summary>
    /// Resolves a command the player typed in chat, e.g. "Sit", to its id for this pet type.
    /// Returns null when the words are not a command, or not one this type knows.
    /// </summary>
    int? ResolveCommandIdByName(int petType, string name);

    Task ReloadAsync(CancellationToken ct);
}
