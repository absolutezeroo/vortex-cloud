using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Vortex.Database.Configuration;

/// <summary>
/// Rejects an unset or placeholder connection string at startup rather than letting every DB call
/// fail later with a driver-level error.
/// <para>
/// Messages here never include the connection string itself — it carries the database password, and
/// options-validation failures are written to the log and to the console.
/// </para>
/// </summary>
public sealed class DatabaseConfigValidator : IValidateOptions<DatabaseConfig>
{
    private static readonly string[] PLACEHOLDER_MARKERS =
    [
        "CHANGE_ME",
        "REPLACE_ME",
        "YOUR_CONNECTION",
        "PLACEHOLDER",
    ];

    public ValidateOptionsResult Validate(string? name, DatabaseConfig options)
    {
        List<string> failures = [];
        string optionPath =
            $"{DatabaseConfig.SECTION_NAME}:{nameof(DatabaseConfig.ConnectionString)}";

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add(
                $"'{optionPath}' is not set. Supply it via the "
                    + "VORTEX__Vortex__Database__ConnectionString environment variable or user-secrets."
            );
        }
        else
        {
            foreach (string marker in PLACEHOLDER_MARKERS)
            {
                if (options.ConnectionString.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add(
                        $"'{optionPath}' is still the placeholder shipped in appsettings.json. "
                            + "Supply the real connection string via the "
                            + "VORTEX__Vortex__Database__ConnectionString environment variable or "
                            + "user-secrets. (The value is deliberately not echoed here — it "
                            + "contains the database password.)"
                    );

                    break;
                }
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
