using System.Threading;
using System.Threading.Tasks;

namespace Turbo.Primitives.Permissions;

public interface ISanctionPresetService
{
    /// <summary>Null if no preset is configured for this (kind, presetIndex) pair — the client sent
    /// an index the server doesn't recognize.</summary>
    Task<SanctionPresetSnapshot?> ResolveAsync(
        SanctionPresetKind kind,
        int presetIndex,
        CancellationToken ct = default
    );
}
