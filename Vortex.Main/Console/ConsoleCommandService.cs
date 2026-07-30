using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Vortex.Plugins;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Admin;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Main.Console;

public class ConsoleCommandService(IServiceProvider services)
{
    private readonly IServiceProvider _services = services;
    private readonly CancellationTokenSource _cts = new();

    private Task? _loopTask;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public void Enable()
    {
        System.Console.WriteLine("Console command service started. Type 'help' for commands.");

        if (IsRunning)
        {
            throw new InvalidOperationException("Already running.");
        }

        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    public async Task DisableAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);

        if (_loopTask is not null)
#pragma warning disable VSTHRD003
        {
            await _loopTask.ConfigureAwait(false);
        }
#pragma warning restore VSTHRD003

        _cts.Dispose();
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string? input = await Task.Run(System.Console.ReadLine, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            await HandleCommandAsync(input, ct).ConfigureAwait(false);
        }
    }

    private async Task HandleCommandAsync(string input, CancellationToken ct)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToLowerInvariant();
        string[] args = parts.Skip(1).ToArray();

        switch (cmd)
        {
            case "help":
                System.Console.WriteLine(
                    "Available commands: help, quit, reload-plugins, reload-plugin <key>, "
                        + "mystery-key <username> <colour>, mystery-box <username> <colour>, "
                        + "reload-mystery-box"
                );
                break;

            case "quit":
            case "exit":
                System.Console.WriteLine("Shutting down...");
                Environment.Exit(0);
                break;

            case "reload-plugins":
                try
                {
                    PluginManager pluginMgr = _services.GetRequiredService<PluginManager>();
                    await pluginMgr.LoadAllAsync(true, ct).ConfigureAwait(false);
                    System.Console.WriteLine("Plugins reloaded.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Reload failed: {ex.Message}");
                }
                break;

            case "reload-plugin":
            {
                if (args.Length == 0)
                {
                    System.Console.WriteLine("Usage: reload-plugin <key>");
                    break;
                }

                try
                {
                    PluginManager pluginMgr = _services.GetRequiredService<PluginManager>();
                    await pluginMgr.ReloadAsync(args[0], ct).ConfigureAwait(false);
                    System.Console.WriteLine($"Plugin '{args[0]}' reloaded.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Reload failed for '{args[0]}': {ex.Message}");
                }
                break;
            }

            // Mystery box keys are not furniture, so there is no catalogue or inventory route to
            // hand one out — this is the operator's grant path (and what a quest reward or a
            // dashboard action would call into).
            case "mystery-key":
            {
                if (args.Length < 2)
                {
                    System.Console.WriteLine(
                        "Usage: mystery-key <username> <colour>  (colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                            + ")"
                    );
                    break;
                }

                string colour = MysteryBoxColors.Normalize(args[1]);

                if (colour.Length == 0)
                {
                    System.Console.WriteLine(
                        $"'{args[1]}' is not a colour the client can render. Valid colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                    );
                    break;
                }

                try
                {
                    IGrainFactory grains = _services.GetRequiredService<IGrainFactory>();
                    PlayerId? playerId = await grains
                        .GetPlayerDirectoryGrain()
                        .GetPlayerIdAsync(args[0], ct)
                        .ConfigureAwait(false);

                    if (playerId is null)
                    {
                        System.Console.WriteLine($"No player named '{args[0]}'.");
                        break;
                    }

                    MysteryBoxAdminResult result = await _services
                        .GetRequiredService<IMysteryBoxAdminService>()
                        .GrantKeyAsync(playerId.Value.Value, colour, "console", ct)
                        .ConfigureAwait(false);

                    System.Console.WriteLine(
                        result.Success
                            ? $"Gave {args[0]} a {colour} mystery box key."
                            : $"Could not give {args[0]} a key: {result.ErrorCode}"
                    );
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"mystery-key failed: {ex.Message}");
                }
                break;
            }

            // The generic item grant can create the same furniture, but only from a raw definition
            // id and without refreshing the recipient's tracker; this picks the box by colour.
            case "mystery-box":
            {
                if (args.Length < 2)
                {
                    System.Console.WriteLine(
                        "Usage: mystery-box <username> <colour>  (colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                            + ")"
                    );
                    break;
                }

                string boxColour = MysteryBoxColors.Normalize(args[1]);

                if (boxColour.Length == 0)
                {
                    System.Console.WriteLine(
                        $"'{args[1]}' is not a colour the client can render. Valid colours: "
                            + string.Join(", ", MysteryBoxColors.All)
                    );
                    break;
                }

                try
                {
                    IMysteryBoxAdminService admin =
                        _services.GetRequiredService<IMysteryBoxAdminService>();
                    IGrainFactory grains = _services.GetRequiredService<IGrainFactory>();

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
                        System.Console.WriteLine(
                            "No furniture definition runs the mystery box logic."
                        );
                        break;
                    }

                    PlayerId? boxPlayerId = await grains
                        .GetPlayerDirectoryGrain()
                        .GetPlayerIdAsync(args[0], ct)
                        .ConfigureAwait(false);

                    if (boxPlayerId is null)
                    {
                        System.Console.WriteLine($"No player named '{args[0]}'.");
                        break;
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

                    System.Console.WriteLine(
                        result.Success
                            ? $"Gave {args[0]} a {boxColour} mystery box."
                            : $"Could not give {args[0]} a box: {result.ErrorCode}"
                    );
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"mystery-box failed: {ex.Message}");
                }
                break;
            }

            case "reload-mystery-box":
                try
                {
                    IGrainFactory grains = _services.GetRequiredService<IGrainFactory>();
                    await grains.GetMysteryBoxManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
                    System.Console.WriteLine("Mystery box definitions and prize pools reloaded.");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Reload failed: {ex.Message}");
                }
                break;

            default:
                System.Console.WriteLine($"Unknown command: {cmd}");
                break;
        }
    }
}
