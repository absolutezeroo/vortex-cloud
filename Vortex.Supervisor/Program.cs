using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Logging.Extensions;
using Vortex.Supervisor.Configuration;
using Vortex.Supervisor.Console;
using Vortex.Supervisor.Endpoints;
using Vortex.Supervisor.Health;
using Vortex.Supervisor.Process;

namespace Vortex.Supervisor;

/// <summary>
///     A deliberately small process whose only job is to outlive the emulator: start it, stop it,
///     restart it, and carry its console both ways. The dashboard stays inside the emulator, where
///     it can keep talking to live services — this exists because nothing living inside a process
///     can restart that process.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Pinned to this assembly's directory rather than the current working directory, which
        // varies by launch method and would otherwise decide which appsettings.json is read.
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory }
        );

        builder.Configuration.AddEnvironmentVariables(prefix: "VORTEX__");

        builder.Logging.ClearProviders();
        builder.Logging.AddVortexConsoleLogger();

        // The health probe polls every few seconds and HttpClient logs two INFO lines per request,
        // which buries the emulator's own output under the supervisor's heartbeat.
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

        builder
            .Services.AddOptions<SupervisorConfig>()
            .Bind(builder.Configuration.GetSection(SupervisorConfig.SECTION_NAME))
            .ValidateOnStart();

        builder.Services.AddSingleton<
            IValidateOptions<SupervisorConfig>,
            SupervisorConfigValidator
        >();

        SupervisorConfig config =
            builder.Configuration.GetSection(SupervisorConfig.SECTION_NAME).Get<SupervisorConfig>()
            ?? new SupervisorConfig();

        builder.Services.AddSingleton(new ConsoleBuffer(Math.Max(1, config.ConsoleBufferLines)));
        builder.Services.AddSingleton<IChildProcessFactory, SystemChildProcessFactory>();
        builder.Services.AddSingleton<EmulatorProcess>();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<EmulatorHealthProbe>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EmulatorHealthProbe>());

        builder.WebHost.UseUrls($"http://{config.Host}:{config.Port}");

        WebApplication app = builder.Build();

        // Logged before the config is validated, not after: the most common failure here is a token
        // set in an appsettings.{Environment}.json this process never layered on, and the refusal
        // that follows cannot say which file it did read. Announcing the environment first turns
        // "the token is not taken into account" into a one-line answer.
        app.Logger.LogInformation(
            "Vortex supervisor starting in the {Environment} environment, reading configuration "
                + "from {ContentRoot}",
            builder.Environment.EnvironmentName,
            AppContext.BaseDirectory
        );

        // Resolving the options runs the validator (placeholder token, cleartext off-box bind)
        // before a single request is served.
        _ = app.Services.GetRequiredService<IOptions<SupervisorConfig>>().Value;

        app.MapSupervisorEndpoints();

        string webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        if (Directory.Exists(webRoot))
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        app.Logger.LogInformation(
            "Vortex supervisor listening on http://{Host}:{Port}",
            config.Host,
            config.Port
        );

        await app.RunAsync().ConfigureAwait(false);

        return 0;
    }
}
