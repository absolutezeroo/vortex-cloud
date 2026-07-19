using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Vortex.Events.Registry;
using Vortex.Players.Configuration;
using Vortex.Players.Events;
using Vortex.Primitives.Events;
using Xunit;

namespace Vortex.Rooms.Tests.Events;

public sealed class GroupNameValidationBehaviorTests
{
    private static GroupNameValidationBehavior CreateBehavior(int maxNameLength = 50) =>
        new(Options.Create(new GroupConfig { MaxNameLength = maxNameLength }));

    private static EventContext CreateContext() => new() { CorrelationId = "test" };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvokeAsync_CancelsOnEmptyOrWhitespaceName(string name)
    {
        GroupNameValidationBehavior behavior = CreateBehavior();
        EventContext ctx = CreateContext();
        bool nextCalled = false;

        await behavior
            .InvokeAsync(
                new GroupCreatingEvent(1, name, 10, 10),
                ctx,
                () =>
                {
                    nextCalled = true;
                    return ValueTask.CompletedTask;
                },
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ctx.Cancel.Should().BeTrue();
        ctx.CancelReason.Should().Be("empty_name");
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_CancelsOnNameExceedingMaxLength()
    {
        GroupNameValidationBehavior behavior = CreateBehavior(maxNameLength: 5);
        EventContext ctx = CreateContext();

        await behavior
            .InvokeAsync(
                new GroupCreatingEvent(1, "TooLongName", 10, 10),
                ctx,
                () => ValueTask.CompletedTask,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ctx.Cancel.Should().BeTrue();
        ctx.CancelReason.Should().Be("name_too_long");
    }

    [Fact]
    public async Task InvokeAsync_DoesNotCancel_ForValidName()
    {
        GroupNameValidationBehavior behavior = CreateBehavior();
        EventContext ctx = CreateContext();
        bool nextCalled = false;

        await behavior
            .InvokeAsync(
                new GroupCreatingEvent(1, "Valid Guild", 10, 10),
                ctx,
                () =>
                {
                    nextCalled = true;
                    return ValueTask.CompletedTask;
                },
                CancellationToken.None
            )
            .ConfigureAwait(true);

        ctx.Cancel.Should().BeFalse();
        nextCalled.Should().BeTrue();
    }
}
