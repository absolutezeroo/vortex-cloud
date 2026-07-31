using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;

namespace Vortex.Plugins.Configuration;

/// <summary>
/// Validates the plugin loader options at startup. A malformed path or debounce here means plugins
/// silently never load (or reload in a tight loop), which is hard to diagnose from the outside.
/// </summary>
public sealed class PluginConfigValidator : IValidateOptions<PluginConfig>
{
    public ValidateOptionsResult Validate(string? name, PluginConfig options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.PluginFolderPath))
        {
            failures.Add(
                $"'{PluginConfig.SECTION_NAME}:{nameof(PluginConfig.PluginFolderPath)}' must not be "
                    + "empty. Remove the key to use the default (<app>/plugins)."
            );
        }
        else if (!IsValidPath(options.PluginFolderPath))
        {
            failures.Add(
                $"'{PluginConfig.SECTION_NAME}:{nameof(PluginConfig.PluginFolderPath)}' is not a "
                    + $"valid filesystem path: '{options.PluginFolderPath}'."
            );
        }

        if (options.DebounceMs < 0)
        {
            failures.Add(
                $"'{PluginConfig.SECTION_NAME}:{nameof(PluginConfig.DebounceMs)}' must not be "
                    + $"negative (got {options.DebounceMs})."
            );
        }

        for (int i = 0; i < options.DevPluginPaths.Length; i++)
        {
            string path = options.DevPluginPaths[i];

            if (string.IsNullOrWhiteSpace(path) || !IsValidPath(path))
            {
                failures.Add(
                    $"'{PluginConfig.SECTION_NAME}:{nameof(PluginConfig.DevPluginPaths)}:{i}' is not "
                        + $"a valid filesystem path: '{path}'."
                );
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    /// <summary>
    /// Path syntax only — a dev plugin folder that does not exist yet is normal (it is produced by a
    /// build), and <c>DiscoverPlugins</c> already skips missing directories.
    /// </summary>
    private static bool IsValidPath(string path)
    {
        try
        {
            _ = Path.GetFullPath(path);

            return true;
        }
        catch (Exception ex)
            when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
