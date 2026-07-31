using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Vortex.Authentication.Configuration;

/// <summary>
/// Validates the authentication options at startup.
/// <para>
/// Outside Development a placeholder <see cref="AuthenticationConfig.IpHashSecret" /> makes the
/// hashed IPs recorded in auth events trivially reversible — the hash is only as secret as its key,
/// and the defaults are in the repository. This carries the check that used to live inline in
/// <c>AuthenticationModule.ConfigureServices</c>, so every sensitive option is now validated the
/// same way (<c>ValidateOnStart</c>) instead of one module hand-rolling its own fail-fast.
/// </para>
/// </summary>
public sealed class AuthenticationConfigValidator(IHostEnvironment environment)
    : IValidateOptions<AuthenticationConfig>
{
    /// <summary>Secrets shipped in the repo; useless as HMAC keys because everyone has them.</summary>
    internal static readonly string[] PLACEHOLDER_IP_HASH_SECRETS =
    [
        "local-dev-ip-hash-secret",
        "replace-with-a-development-secret",
        "replace-with-a-production-secret",
    ];

    public ValidateOptionsResult Validate(string? name, AuthenticationConfig options)
    {
        List<string> failures = [];

        if (!environment.IsDevelopment())
        {
            if (
                string.IsNullOrWhiteSpace(options.IpHashSecret)
                || Array.IndexOf(PLACEHOLDER_IP_HASH_SECRETS, options.IpHashSecret) >= 0
            )
            {
                failures.Add(
                    $"'{AuthenticationConfig.SECTION_NAME}:{nameof(AuthenticationConfig.IpHashSecret)}' "
                        + "is unset or still a placeholder default. The IP addresses hashed into auth "
                        + "events would be reversible by anyone with the repository. Set a real "
                        + "secret via the VORTEX__Vortex__Authentication__IpHashSecret environment "
                        + "variable or user-secrets before running outside Development."
                );
            }
        }

        if (options.TicketTtlSeconds < 0)
        {
            failures.Add(
                $"'{AuthenticationConfig.SECTION_NAME}:{nameof(AuthenticationConfig.TicketTtlSeconds)}' "
                    + $"must not be negative (got {options.TicketTtlSeconds}). Use 0 to accept "
                    + "null-expiry tickets unconditionally, or a positive number of seconds."
            );
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
