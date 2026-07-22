using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for the server-config editor. The <c>IServerConfigGrain</c> only knows keys that have
/// actually been written to the database, so each row is the <see cref="ConfigKeyCatalog"/> descriptor
/// (default/kind/description/group) joined with the live stored value — letting the dashboard show every
/// tunable key, its default, and whether an operator has overridden it. Writes live in
/// <c>DashboardOperationsService.Config.cs</c>.
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>Every known config key with its catalog metadata and current stored value (if set).</summary>
    public async Task<object> ConfigListAsync(CancellationToken ct)
    {
        ImmutableDictionary<string, string> current = await _grainFactory
            .GetServerConfigGrain()
            .GetAllAsync()
            .ConfigureAwait(false);

        ImmutableArray<ConfigEntryDto> items =
        [
            .. ConfigKeyCatalog.All.Select(descriptor => new ConfigEntryDto(
                descriptor.Key,
                descriptor.Group,
                descriptor.Description,
                descriptor.Kind.ToString(),
                descriptor.DefaultValue,
                current.TryGetValue(descriptor.Key, out string? value) ? value : null,
                current.ContainsKey(descriptor.Key)
            )),
        ];

        return new { count = items.Length, items };
    }
}

/// <summary>One row in the server-config editor: static catalog metadata plus the current stored
/// override, if any. <see cref="IsOverridden"/> distinguishes "explicitly set to the default value"
/// from "never set" (the grain has no row for it and the default applies).</summary>
public sealed record ConfigEntryDto(
    string Key,
    string Group,
    string Description,
    string Kind,
    string DefaultValue,
    string? CurrentValue,
    bool IsOverridden
);
