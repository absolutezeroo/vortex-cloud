using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vortex.Database.Configuration;
using Vortex.Database.Context;
using Vortex.Database.Migrations;
using Xunit;

namespace Vortex.Database.Tests.Migrations;

/// <summary>
/// The startup hook runs against the production database on every boot, so the only two things
/// worth pinning are that it is genuinely off unless asked -- a flag that silently defaults on
/// would migrate a hotel nobody meant to migrate -- and that switching it on actually reaches the
/// database rather than logging and returning.
///
/// Neither test needs MySQL: "off" is proved by resolving nothing, and "on" by the resolution it
/// is forced to attempt.
/// </summary>
public sealed class MigrationHelperStartupTests
{
    [Fact]
    public async Task Disabled_ByDefault_TouchesNothing()
    {
        // Deliberately bare: no context factory, no logging. Anything the helper resolves when the
        // flag is off would throw here, which is the assertion.
        ServiceProvider sp = new ServiceCollection()
            .AddSingleton<IOptions<DatabaseConfig>>(Options.Create(new DatabaseConfig()))
            .BuildServiceProvider();

        new DatabaseConfig().MigrateOnStartup.Should().BeFalse();

        // Returning at all is the assertion.
        await MigrationHelper.ApplyStartupMigrationsAsync(sp, CancellationToken.None);
    }

    [Fact]
    public async Task Enabled_ReachesForTheContextFactory()
    {
        ServiceProvider sp = new ServiceCollection()
            .AddSingleton<IOptions<DatabaseConfig>>(
                Options.Create(new DatabaseConfig { MigrateOnStartup = true })
            )
            .AddLogging()
            .BuildServiceProvider();

        Func<Task> act = () =>
            MigrationHelper.ApplyStartupMigrationsAsync(sp, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should()
            .Contain(nameof(IDbContextFactory<VortexDbContext>));
    }
}
