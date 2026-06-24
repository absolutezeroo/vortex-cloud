using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Primitives.Pets.Providers;

public interface IPetPaletteProvider
{
    IReadOnlyList<PetPaletteEntry> GetPalettesForType(int petType);

    Task ReloadAsync(CancellationToken ct);
}
