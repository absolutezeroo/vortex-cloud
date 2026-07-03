using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Authentication.Configuration;
using Turbo.Authentication.Permissions;
using Turbo.Primitives.Authentication;
using Turbo.Primitives.Permissions;
using Turbo.Primitives.Plugins;

namespace Turbo.Authentication;

public sealed class AuthenticationModule : IHostPluginModule
{
    private static readonly string[] DefaultIpHashSecrets =
    [
        "local-dev-ip-hash-secret",
        "replace-with-a-development-secret",
        "replace-with-a-production-secret",
    ];

    public string Key => "turbo-authentication";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<AuthenticationConfig>(
            builder.Configuration.GetSection(AuthenticationConfig.SECTION_NAME)
        );

        // Fail fast outside Development if the placeholder IP-hash secret was never overridden
        // (via TURBO__Turbo__Authentication__IpHashSecret or user-secrets) — a default secret here
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
                "Turbo:Authentication:IpHashSecret is unset or still a placeholder default. "
                    + "Set a real secret via the TURBO__Turbo__Authentication__IpHashSecret "
                    + "environment variable or user-secrets before running outside Development."
            );
        }

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAccountAuthenticator, AccountAuthenticator>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddHostedService<PermissionSeederService>();
    }
}
