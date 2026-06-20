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
    public string Key => "turbo-authentication";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<AuthenticationConfig>(
            builder.Configuration.GetSection(AuthenticationConfig.SECTION_NAME)
        );

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAccountAuthenticator, AccountAuthenticator>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddHostedService<PermissionSeederService>();
    }
}
