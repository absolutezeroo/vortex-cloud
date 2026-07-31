using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Hosting;

namespace Vortex.Observability.Configuration;

/// <summary>
/// Validates the admin dashboard's listener configuration at startup. The dashboard session cookie
/// grants <c>dashboard.*</c> capabilities, so a cleartext off-box listener hands full operator
/// access to anyone on the network path — that must be a startup error, not a silent default.
/// </summary>
public sealed class ObservabilityConfigValidator(IHostEnvironment environment)
    : IValidateOptions<ObservabilityConfig>
{
    private const string SERVICE_NAME = "Vortex dashboard API";

    public ValidateOptionsResult Validate(string? name, ObservabilityConfig options)
    {
        // A disabled dashboard opens no socket, so none of this can bite.
        if (!options.DashboardEnabled)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        if (options.DashboardPort is < 1 or > 65535)
        {
            failures.Add(
                $"'{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardPort)}' "
                    + $"must be between 1 and 65535 (got {options.DashboardPort})."
            );
        }

        if (options.DashboardHttpsEnabled && options.DashboardHttpsPort is < 1 or > 65535)
        {
            failures.Add(
                $"'{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardHttpsPort)}' "
                    + $"must be between 1 and 65535 (got {options.DashboardHttpsPort})."
            );
        }

        if (options.DashboardSessionLifetimeMinutes < 5)
        {
            failures.Add(
                $"'{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardSessionLifetimeMinutes)}' "
                    + $"must be at least 5 minutes (got {options.DashboardSessionLifetimeMinutes})."
            );
        }

        ListenerSecurityResult listener = ListenerSecurity.ValidateListener(
            SERVICE_NAME,
            options.DashboardHost,
            options.DashboardPort,
            options.DashboardHttpsEnabled,
            options.DashboardAllowInsecureRemoteHttp,
            $"{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardAllowInsecureRemoteHttp)}"
        );

        if (!listener.IsAllowed)
        {
            failures.Add(listener.Message!);
        }

        ListenerSecurityResult certificate = HttpsCertificateValidator.ValidateCertificate(
            SERVICE_NAME,
            options.DashboardHttpsEnabled,
            options.DashboardCertificatePath,
            options.DashboardCertificatePassword,
            environment.IsDevelopment(),
            $"{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardCertificatePath)}"
        );

        if (!certificate.IsAllowed)
        {
            failures.Add(certificate.Message!);
        }

        if (
            options.DashboardUseForwardedHeaders
            && options.DashboardKnownProxies.Length == 0
            && options.DashboardKnownNetworks.Length == 0
        )
        {
            failures.Add(
                $"'{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardUseForwardedHeaders)}' "
                    + "is enabled but no trusted proxy is configured. Without an allow-list any "
                    + "client could forge X-Forwarded-For (defeating the login rate limit and "
                    + "poisoning the audit trail) and X-Forwarded-Proto. Populate "
                    + $"'{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardKnownProxies)}' "
                    + $"or '{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardKnownNetworks)}'."
            );
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
