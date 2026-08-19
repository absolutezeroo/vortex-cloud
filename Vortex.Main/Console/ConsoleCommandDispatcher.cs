using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Vortex.Plugins;
using Vortex.Primitives.Console;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Admin;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Players;

namespace Vortex.Main.Console;

/// <summary>
/// The operator command set, independent of where the command came from. <see
/// cref="ConsoleCommandService"/> feeds it stdin; the dashboard console feeds it a typed request
/// from an authenticated staff member. Output goes to the caller's sink rather than straight to
/// <c>System.Console</c>, which is what lets the same command answer both.
/// </summary>
public sealed class ConsoleCommandDispatcher(IServiceProvider services) : IConsoleCommandDispatcher
{
    private static readonly ConsoleCommandDescriptor[] CommandList =
    [
        new("help", "help", "List the available commands."),
        new(
            "quit",
            "quit",
            "Shut the emulator down gracefully.",
            Capabilities.Dashboard.OpsServerControl,
            ["exit"]
        ),
        new(
            "reload-plugins",
            "reload-plugins",
            "Reload every plugin from disk.",
            Capabilities.Dashboard.OpsConfigManage
        ),
        new(
            "reload-plugin",
            "reload-plugin <key>",
            "Reload a single plugin by key.",
            Capabilities.Dashboard.OpsConfigManage
        ),
        new(
            "mystery-key",
            "mystery-key <username> <colour>",
            "Give a player a mystery box key.",
            Capabilities.Dashboard.OpsMysteryBoxManage
        ),
        new(
            "mystery-box",
            "mystery-box <username> <colour>",
            "Give a player a mystery box.",
            Capabilities.Dashboard.OpsMysteryBoxManage
        ),
        new(
            "reload-mystery-box",
            "reload-mystery-box",
            "Reload mystery box definitions and prize pools.",
            Capabilities.Dashboard.OpsMysteryBoxManage
        ),
    ];

    public IReadOnlyList<ConsoleCommandDescriptor> Commands => CommandList;

    public ConsoleCommandDescriptor? Find(string name)
    {
        string needle = name.Trim().ToLowerInvariant();

        return Array.Find(
            CommandList,
            c =>
                c.Name == needle
                || (c.Aliases is not null && c.Aliases.Contains(needle, StringComparer.Ordinal))
        );
    }

    public async Task<bool> ExecuteAsync(string input, Action<string> write, CancellationToken ct)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return true;
        }

        string cmd = parts[0].ToLowerInvariant();
        string[] args = [.. parts.Skip(1)];

        switch (cmd)
        {
            case "help":
                write("Available commands:");

                foreach (ConsoleCommandDescriptor descriptor in CommandList)
                {
                    write($"  {descriptor.Usage, -38} {descriptor.Description}");
                }

                return true;

            case "quit":
            case "exit":
                // Environment.Exit(0) used to cut the process here, which skipped host shutdown
                // entirely: no hosted service got to stop, and anything still buffered was lost.
                // Stopping the application lets the shutdown path run and the process exit 0 on
                // its own.
                write("Shutting down...");
                services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

                return true;

            case "reload-plugins":
                try
                {
                    PluginManager pluginMgr = services.GetRequiredService<PluginManager>();
                    await pluginMgr.LoadAllAsync(true, ct).ConfigureAwait(false);
                    write("Plugins reloaded.");
                }
                catch (Exception ex)
                {
                    write($"Reload failed: {ex.Message}");
                }

                return true;

            case "reload-plugin":
            {
                if (args.Length == 0)
                {
                    write("Usage: reload-plugin <key>");

                    return true;
                }

                try
                {
                    PluginManager pluginMgr = services.GetRequiredService<PluginManager>();
                    await pluginMgr.ReloadAsync(args[0], ct).ConfigureAwait(false);
                    write($"Plugin '{args[0]}' reloaded.");
                }
                catch (Exception ex)
                {
                    write($"Reload failed for '{args[0]}': {ex.Message}");
                }

                return true;
            }

            // Mystery box keys are not furniture, so there is no catalogue or inventory route to
            // hand one out — this is the operator's grant path (and what a quest reward or a
            // dashboard action would call into).
            case "mystery-key":
            {
                if (args.Length < 2)
                {
                    write(
                        "Usage: mystery-key <username> <colour>  (colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                            + ")"
                    );

                    return true;
                }

                string colour = MysteryBoxColors.Normalize(args[1]);

                if (colour.Length == 0)
                {
                    write(
                        $"'{args[1]}' is not a colour the client can render. Valid colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                    );

                    return true;
                }

                try
                {
                    IGrainFactory grains = services.GetRequiredService<IGrainFactory>();
                    PlayerId? playerId = await grains
                        .GetPlayerDirectoryGrain()
                        .GetPlayerIdAsync(args[0], ct)
                        .ConfigureAwait(false);

                    if (playerId is null)
                    {
                        write($"No player named '{args[0]}'.");

                        return true;
                    }

                    MysteryBoxAdminResult result = await services
                        .GetRequiredService<IMysteryBoxAdminService>()
                        .GrantKeyAsync(playerId.Value.Value, colour, "console", ct)
                        .ConfigureAwait(false);

                    write(
                        result.Success
                            ? $"Gave {args[0]} a {colour} mystery box key."
                            : $"Could not give {args[0]} a key: {result.ErrorCode}"
                    );
                }
                catch (Exception ex)
                {
                    write($"mystery-key failed: {ex.Message}");
                }

                return true;
            }

            // The generic item grant can create the same furniture, but only from a raw definition
            // id and without refreshing the recipient's tracker; this picks the box by colour.
            case "mystery-box":
            {
                if (args.Length < 2)
                {
                    write(
                        "Usage: mystery-box <username> <colour>  (colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                            + ")"
                    );

                    return true;
                }

                string boxColour = MysteryBoxColors.Normalize(args[1]);

                if (boxColour.Length == 0)
                {
                    write(
                        $"'{args[1]}' is not a colour the client can render. Valid colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                    );

                    return true;
                }

                try
                {
                    IMysteryBoxAdminService admin =
                        services.GetRequiredService<IMysteryBoxAdminService>();
                    IGrainFactory grains = services.GetRequiredService<IGrainFactory>();

                    // Any furniture running the mystery box logic will do; the colour is baked into
                    // the item's state, not into the definition.
                    int definitionId = (
                        await grains
                            .GetMysteryBoxManagerGrain()
                            .GetBoxDefinitionIdsAsync(ct)
                            .ConfigureAwait(false)
                    ).FirstOrDefault();

                    if (definitionId <= 0)
                    {
                        write("No furniture definition runs the mystery box logic.");

                        return true;
                    }

                    PlayerId? boxPlayerId = await grains
                        .GetPlayerDirectoryGrain()
                        .GetPlayerIdAsync(args[0], ct)
                        .ConfigureAwait(false);

                    if (boxPlayerId is null)
                    {
                        write($"No player named '{args[0]}'.");

                        return true;
                    }

                    MysteryBoxAdminResult result = await admin
                        .GrantBoxAsync(
                            boxPlayerId.Value.Value,
                            definitionId,
                            boxColour,
                            "console",
                            ct
                        )
                        .ConfigureAwait(false);

                    write(
                        result.Success
                            ? $"Gave {args[0]} a {boxColour} mystery box."
                            : $"Could not give {args[0]} a box: {result.ErrorCode}"
                    );
                }
                catch (Exception ex)
                {
                    write($"mystery-box failed: {ex.Message}");
                }

                return true;
            }

            case "reload-mystery-box":
                try
                {
                    IGrainFactory grains = services.GetRequiredService<IGrainFactory>();
                    await grains.GetMysteryBoxManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
                    write("Mystery box definitions and prize pools reloaded.");
                }
                catch (Exception ex)
                {
                    write($"Reload failed: {ex.Message}");
                }

                return true;

            default:
                write($"Unknown command: {cmd}");

                return false;
        }
    }
}
