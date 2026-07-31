using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Authentication.Configuration;
using Vortex.Authentication.Moderation;
using Vortex.Authentication.Permissions;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Plugins;

namespace Vortex.Authentication;

public sealed class AuthenticationModule : IHostPluginModule
{
    public string Key => "vortex-authentication";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        // The placeholder-IpHashSecret fail-fast that used to be inlined here now lives in
        // AuthenticationConfigValidator, so every sensitive option is validated the same way
        // (ValidateOnStart) instead of this one module hand-rolling its own check. Behaviour is
        // unchanged: outside Development a placeholder secret still refuses to start.
        services
            .AddOptions<AuthenticationConfig>()
            .Bind(builder.Configuration.GetSection(AuthenticationConfig.SECTION_NAME))
            .ValidateOnStart();

        services.AddSingleton<
            IValidateOptions<AuthenticationConfig>,
            AuthenticationConfigValidator
        >();

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
