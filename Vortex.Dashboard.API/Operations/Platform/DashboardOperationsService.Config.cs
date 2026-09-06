using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Server;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Write surface for the server-config editor. Every set routes through <c>IServerConfigGrain</c>
/// (write-through DB + live cache, never a direct DB write), is gated to a known
/// <see cref="ConfigKeyCatalog"/> key, and has its value validated against that key's declared kind
/// before it lands — an unknown key or an unparseable value is surfaced to the operator as a
/// domain-validation failure rather than a generic fault.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> SetConfigAsync(
        SetConfigRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.config.set",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Key, request.Value },
            work: async _ =>
            {
                ConfigKeyDescriptor descriptor =
                    ConfigKeyCatalog.Find(request.Key)
                    ?? throw new InvalidOperationException("unknown_config_key");

                if (!IsValidValue(descriptor.Kind, request.Value))
                {
                    throw new InvalidOperationException("invalid_value");
                }

                await _grainFactory
                    .GetServerConfigGrain()
                    .SetValueAsync(request.Key, request.Value, descriptor.Description)
                    .ConfigureAwait(false);
            },
            ct
        );

    private static bool IsValidValue(ConfigValueKind kind, string value) =>
        kind switch
        {
            ConfigValueKind.Int => int.TryParse(value, out _),
            ConfigValueKind.Bool => bool.TryParse(value, out _),
            ConfigValueKind.Json => IsValidJson(value),
            _ => true,
        };

    private static bool IsValidJson(string value)
    {
        try
        {
            using JsonDocument _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
