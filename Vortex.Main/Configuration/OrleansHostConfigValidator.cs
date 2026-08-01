using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Options;
using Vortex.Database.Configuration;

namespace Vortex.Main.Configuration;

/// <summary>
/// Validates the Orleans silo endpoint configuration at startup. A bad advertised IP or a port
/// collision here surfaces as an opaque clustering failure well after the host has started
/// announcing itself.
/// </summary>
public sealed class OrleansHostConfigValidator(IOptions<DatabaseConfig> databaseConfig)
    : IValidateOptions<OrleansHostConfig>
{
    private static readonly string[] KnownClusteringProviders = ["localhost", "adonet"];
    private static readonly string[] KnownGrainStorageProviders = ["memory", "adonet"];

    public ValidateOptionsResult Validate(string? name, OrleansHostConfig options)
    {
        List<string> failures = [];

        if (Array.IndexOf(KnownClusteringProviders, options.ClusteringProvider) < 0)
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.ClusteringProvider)}' "
                    + $"must be one of: {string.Join(", ", KnownClusteringProviders)} "
                    + $"(got '{options.ClusteringProvider}')."
            );
        }

        if (Array.IndexOf(KnownGrainStorageProviders, options.GrainStorageProvider) < 0)
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.GrainStorageProvider)}' "
                    + $"must be one of: {string.Join(", ", KnownGrainStorageProviders)} "
                    + $"(got '{options.GrainStorageProvider}')."
            );
        }

        if (
            (options.ClusteringProvider == "adonet" || options.GrainStorageProvider == "adonet")
            && string.IsNullOrWhiteSpace(options.Invariant)
        )
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.Invariant)}' must be "
                    + "set when clustering or grain storage uses the 'adonet' provider."
            );
        }

        if (
            (options.ClusteringProvider == "adonet" || options.GrainStorageProvider == "adonet")
            && string.IsNullOrWhiteSpace(databaseConfig.Value.ConnectionString)
        )
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}' selects the 'adonet' provider, but "
                    + $"'{DatabaseConfig.SECTION_NAME}:{nameof(DatabaseConfig.ConnectionString)}' is "
                    + "not set. Orleans reuses that connection string rather than a separate one."
            );
        }

        if (!IPAddress.TryParse(options.AdvertisedIp, out _))
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.AdvertisedIp)}' must be "
                    + $"a literal IP address (got '{options.AdvertisedIp}'). Orleans advertises this "
                    + "address to other silos and to gateway clients; host names are not resolved."
            );
        }

        if (options.SiloPort is < 1 or > 65535)
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.SiloPort)}' must be "
                    + $"between 1 and 65535 (got {options.SiloPort})."
            );
        }

        if (options.GatewayPort is < 1 or > 65535)
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.GatewayPort)}' must be "
                    + $"between 1 and 65535 (got {options.GatewayPort})."
            );
        }

        if (options.SiloPort == options.GatewayPort)
        {
            failures.Add(
                $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.SiloPort)}' and "
                    + $"'{OrleansHostConfig.SECTION_NAME}:{nameof(OrleansHostConfig.GatewayPort)}' "
                    + $"are both {options.SiloPort}; the silo and gateway need separate ports."
            );
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
