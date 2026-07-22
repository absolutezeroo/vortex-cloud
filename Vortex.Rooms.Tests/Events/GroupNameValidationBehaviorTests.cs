using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Players.Events;
using Vortex.Primitives.Events;
using Vortex.Primitives.Server.Grains;
using Xunit;

namespace Vortex.Rooms.Tests.Events;

public sealed class GroupNameValidationBehaviorTests
{
    private static GroupNameValidationBehavior CreateBehavior(int maxNameLength = 50) =>
        new(CreateGrainFactory(maxNameLength));

    private static IGrainFactory CreateGrainFactory(int maxNameLength)
    {
        IGrainFactory grainFactory = DispatchProxy.Create<IGrainFactory, ConfigGrainFactoryProxy>();
        ((ConfigGrainFactoryProxy)(object)grainFactory).Config = new FakeServerConfigGrain(
            maxNameLength
        );

        return grainFactory;
    }

    private class ConfigGrainFactoryProxy : DispatchProxy
    {
        public FakeServerConfigGrain Config { get; set; } = default!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (
                targetMethod is not null
                && targetMethod.Name == "GetGrain"
                && targetMethod.GetGenericArguments()[0] == typeof(IServerConfigGrain)
            )
            {
                return Config;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private sealed class FakeServerConfigGrain(int intValue) : IServerConfigGrain
    {
        public Task<int> GetIntAsync(string key, int fallback) => Task.FromResult(intValue);

        public Task<string?> GetValueAsync(string key) => Task.FromResult<string?>(null);

        public Task<bool> GetBoolAsync(string key, bool fallback) => Task.FromResult(fallback);

        public Task<ImmutableDictionary<string, string>> GetAllAsync() =>
            Task.FromResult(ImmutableDictionary<string, string>.Empty);

        public Task SetValueAsync(string key, string value, string? description) =>
            Task.CompletedTask;

        public Task ReloadAsync() => Task.CompletedTask;

        public Task<ImmutableArray<string>> GetMotdAsync() =>
            Task.FromResult(ImmutableArray<string>.Empty);

        public Task SetMotdAsync(ImmutableArray<string> lines) => Task.CompletedTask;
    }

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
