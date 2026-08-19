using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Hosting;

namespace Vortex.Supervisor.Configuration;

/// <summary>
///     Turns the two ways this listener can be dangerous into named startup errors. It is the one
///     surface that can stop the hotel and stream its logs, and it is reachable while everything
///     else is down — so a placeholder token or a cleartext off-box bind is refused rather than
///     warned about.
/// </summary>
public sealed class SupervisorConfigValidator(IHostEnvironment environment)
    : IValidateOptions<SupervisorConfig>
{
    /// <summary>Short enough to brute-force over a network in reasonable time.</summary>
    private const int MINIMUM_TOKEN_LENGTH = 24;

    public ValidateOptionsResult Validate(string? name, SupervisorConfig options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Token' is empty. The supervisor can stop the "
                    + "hotel and stream its console; it will not serve without a secret."
            );
        }
        else if (
            options.Token.Equals(SupervisorConfig.PLACEHOLDER_TOKEN, StringComparison.Ordinal)
            || options.Token.StartsWith("CHANGE_ME", StringComparison.Ordinal)
        )
        {
            // The shipped value is in the repository, so anyone who has read it holds the key to
            // every deployment that never replaced it.
            //
            // Naming the environment matters more than it looks: a token set in
            // appsettings.Development.json while the process runs as Production is never layered on,
            // and the operator sees "still the placeholder" for a value they demonstrably set.
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Token' is still the placeholder shipped in "
                    + $"appsettings.json. This process is running in the "
                    + $"'{environment.EnvironmentName}' environment, so it layered "
                    + $"appsettings.json and appsettings.{environment.EnvironmentName}.json only — a "
                    + "token set in any other environment's file is not read. Generate a fresh "
                    + "secret and set it there, or via the VORTEX__Vortex__Supervisor__Token "
                    + "environment variable (set DOTNET_ENVIRONMENT to pick the environment)."
            );
        }
        else if (options.Token.Length < MINIMUM_TOKEN_LENGTH)
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Token' is {options.Token.Length} characters; at "
                    + $"least {MINIMUM_TOKEN_LENGTH} are required."
            );
        }

        ListenerSecurityResult listener = ListenerSecurity.ValidateListener(
            "the Vortex supervisor",
            options.Host,
            options.Port,
            httpsEnabled: false,
            options.AllowInsecureRemoteHttp,
            $"{SupervisorConfig.SECTION_NAME}:AllowInsecureRemoteHttp"
        );

        if (!listener.IsAllowed && listener.Message is not null)
        {
            failures.Add(listener.Message);
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Port' must be between 1 and 65535 "
                    + $"(got {options.Port})."
            );
        }

        if (options.ConsoleBufferLines < 1)
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:ConsoleBufferLines' must be at least 1 "
                    + $"(got {options.ConsoleBufferLines})."
            );
        }

        if (options.Emulator.GracefulShutdownTimeoutSeconds < 1)
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Emulator:GracefulShutdownTimeoutSeconds' must be "
                    + $"at least 1 (got {options.Emulator.GracefulShutdownTimeoutSeconds})."
            );
        }

        if (string.IsNullOrWhiteSpace(options.Emulator.ExecutablePath))
        {
            failures.Add(
                $"'{SupervisorConfig.SECTION_NAME}:Emulator:ExecutablePath' is empty; there is "
                    + "nothing for the supervisor to start."
            );
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
