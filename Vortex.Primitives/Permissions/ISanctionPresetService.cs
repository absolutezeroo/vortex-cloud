using System.Threading;
using System.Threading.Tasks;

namespace Vortex.Primitives.Permissions;

public interface ISanctionPresetService
{
    /// <summary>Null if no preset is configured for this (kind, presetIndex) pair — the client sent
    /// an index the server doesn't recognize.</summary>
    Task<SanctionPresetSnapshot?> ResolveAsync(
        SanctionPresetKind kind,
        int presetIndex,
        CancellationToken ct = default
    );

    /// <summary>Null if no preset exists with this id — used when a preset is referenced by its raw
    /// database id rather than (kind, presetIndex), e.g. CfhTopicEntity.DefaultSanctionPresetEntityId.</summary>
    Task<SanctionPresetSnapshot?> ResolveByIdAsync(int presetId, CancellationToken ct = default);
}
