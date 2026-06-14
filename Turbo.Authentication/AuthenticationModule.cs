using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Contracts.Plugins;
using Turbo.Primitives.Authentication;
using Turbo.Authentication.Configuration;

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
    }
}
