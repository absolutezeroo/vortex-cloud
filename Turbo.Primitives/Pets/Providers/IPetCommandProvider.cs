using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Primitives.Pets.Providers;

public interface IPetCommandProvider
{
    IReadOnlyList<PetCommandEntry> GetCommandsForType(int petType);

    ImmutableArray<int> GetAllCommandIds(int petType);

    ImmutableArray<int> GetEnabledCommandIds(int petType, int petLevel);

    PetCommandEntry? GetCommandConfig(int petType, int commandId);

    Task ReloadAsync(CancellationToken ct);
}
