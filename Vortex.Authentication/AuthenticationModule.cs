using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Authentication.Configuration;
using Vortex.Authentication.Moderation;
using Vortex.Authentication.Permissions;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Plugins;

namespace Vortex.Authentication;

public sealed class AuthenticationModule : IHostPluginModule
{
    private static readonly string[] DefaultIpHashSecrets =
    [
        "local-dev-ip-hash-secret",
        "replace-with-a-development-secret",
        "replace-with-a-production-secret",
    ];

    public string Key => "vortex-authentication";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<AuthenticationConfig>(
            builder.Configuration.GetSection(AuthenticationConfig.SECTION_NAME)
        );

        // Fail fast outside Development if the placeholder IP-hash secret was never overridden
        // (via VORTEX__Vortex__Authentication__IpHashSecret or user-secrets) — a default secret here
        // would make the hashed IPs in auth events trivially reversible/guessable in production.
        string ipHashSecret =
            builder.Configuration[$"{AuthenticationConfig.SECTION_NAME}:IpHashSecret"]
            ?? string.Empty;

        if (
            !builder.Environment.IsDevelopment()
            && (
                string.IsNullOrEmpty(ipHashSecret)
                || Array.IndexOf(DefaultIpHashSecrets, ipHashSecret) >= 0
            )
        )
        {
            throw new InvalidOperationException(
                "Vortex:Authentication:IpHashSecret is unset or still a placeholder default. "
                    + "Set a real secret via the VORTEX__Vortex__Authentication__IpHashSecret "
                    + "environment variable or user-secrets before running outside Development."
            );
        }

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAccountAuthenticator, AccountAuthenticator>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddSingleton<ISanctionPresetService, SanctionPresetService>();
        services.AddHostedService<PermissionSeederService>();
        services.AddHostedService<SanctionPresetSeederService>();
        // Must run after SanctionPresetSeederService: it links CFH topics to Ban presets by
        // (Kind, PresetIndex), which only exist once that seeder has run. Hosted services start in
        // registration order, so this ordering is load-bearing, not incidental.
        services.AddHostedService<CfhCatalogSeederService>();
    }
}
