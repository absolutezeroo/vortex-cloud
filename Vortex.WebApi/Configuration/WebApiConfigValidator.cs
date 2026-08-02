using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Hosting;

namespace Vortex.WebApi.Configuration;

/// <summary>
/// Validates the client-facing web API's listener configuration at startup, so an insecure or
/// impossible setup is a clear startup error rather than a live cleartext credential endpoint or a
/// listener that quietly never came up.
/// </summary>
public sealed class WebApiConfigValidator(IHostEnvironment environment)
    : IValidateOptions<WebApiConfig>
{
    private const int MinimumMetricsTokenLength = 24;

    public ValidateOptionsResult Validate(string? name, WebApiConfig options)
    {
        // A disabled API opens no socket, so none of this can bite.
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];

        if (options.Port is < 1 or > 65535)
        {
            failures.Add(
                $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.Port)}' must be between 1 and "
                    + $"65535 (got {options.Port})."
            );
        }

        if (options.HttpsEnabled && options.HttpsPort is < 1 or > 65535)
        {
            failures.Add(
                $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.HttpsPort)}' must be between 1 "
                    + $"and 65535 (got {options.HttpsPort})."
            );
        }

        ListenerSecurityResult listener = ListenerSecurity.ValidateListener(
            "Vortex web API",
            options.Host,
            options.Port,
            options.HttpsEnabled,
            options.AllowInsecureRemoteHttp,
            $"{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.AllowInsecureRemoteHttp)}"
        );

        if (!listener.IsAllowed)
        {
            failures.Add(listener.Message!);
        }

        ListenerSecurityResult certificate = HttpsCertificateValidator.ValidateCertificate(
            "Vortex web API",
            options.HttpsEnabled,
            options.CertificatePath,
            options.CertificatePassword,
            environment.IsDevelopment(),
            $"{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.CertificatePath)}"
        );

        if (!certificate.IsAllowed)
        {
            failures.Add(certificate.Message!);
        }

        if (
            options.UseForwardedHeaders
            && options.KnownProxies.Length == 0
            && options.KnownNetworks.Length == 0
        )
        {
            failures.Add(
                $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.UseForwardedHeaders)}' is enabled "
                    + "but no trusted proxy is configured. Without an allow-list any client could "
                    + "forge X-Forwarded-For (defeating the login and registration rate limits) and "
                    + $"X-Forwarded-Proto. Populate "
                    + $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.KnownProxies)}' or "
                    + $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.KnownNetworks)}'."
            );
        }

        if (options.MetricsEnabled && !options.MetricsPath.StartsWith('/'))
        {
            failures.Add(
                $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.MetricsPath)}' must be an "
                    + $"absolute path starting with '/' (got '{options.MetricsPath}')."
            );
        }

        // A short token is worse than no token: it reads as protection while staying guessable,
        // whereas an empty one at least restricts the endpoint to loopback.
        if (
            options.MetricsEnabled
            && options.MetricsToken.Length > 0
            && options.MetricsToken.Length < MinimumMetricsTokenLength
        )
        {
            failures.Add(
                $"'{WebApiConfig.SECTION_NAME}:{nameof(WebApiConfig.MetricsToken)}' must be at least "
                    + $"{MinimumMetricsTokenLength} characters, or empty to restrict the metrics "
                    + "endpoint to loopback callers."
            );
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
