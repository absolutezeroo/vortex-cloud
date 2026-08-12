using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Hosting;
using Vortex.Dashboard.API.Operations;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The filter is the single place that enforces "a dashboard write carries a body and an audited
/// reason". Every write endpoint depends on it now, and the last test is the one that matters most:
/// it fails if a new operation request record is added without the marker interface, which is
/// exactly the mistake that would let an unjustified write through unnoticed.
/// </summary>
public sealed class DashboardRequestValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData(" a ")]
    public async Task RejectsAnUnusableReason(string? reason)
    {
        (await InvokeAsync(new KickPlayerRequest(1, reason!))).Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("  spam  ")]
    [InlineData("Refunded a duplicated purchase")]
    public async Task AcceptsAReasonWithSubstance(string reason)
    {
        (await InvokeAsync(new KickPlayerRequest(1, reason))).Should().BeTrue();
    }

    [Fact]
    public async Task RejectsAMissingBody()
    {
        (await InvokeAsync(new object?[] { null })).Should().BeFalse();
    }

    [Fact]
    public async Task PassesArgumentsItDoesNotOwn()
    {
        (await InvokeAsync(new DefaultHttpContext(), CancellationToken.None, "not a request"))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void EveryOperationRequestDeclaresItsReason()
    {
        // A record that takes an audited reason but does not implement IReasonedRequest would be
        // posted, run and audited with whatever the caller sent -- including an empty string.
        // The reason has to be a *string* to count: CloseCfhTicketsRequest.Reason is an int, the
        // client's close-reason code (1=Useless, 2=Sanctioned, 3=Resolved), not a justification, and
        // closing a ticket is a queue move rather than an audited write.
        IEnumerable<Type> unmarked = typeof(OperationResult)
            .Assembly.GetTypes()
            .Where(t =>
                t is { IsClass: true, Namespace: "Vortex.Dashboard.API.Operations" }
                && t.Name.EndsWith("Request", StringComparison.Ordinal)
                && t.GetProperty("Reason")?.PropertyType == typeof(string)
                && !typeof(IReasonedRequest).IsAssignableFrom(t)
            );

        unmarked.Should().BeEmpty();
    }

    /// <summary>Runs the filter and reports whether the request reached the endpoint.</summary>
    private static async Task<bool> InvokeAsync(params object?[] arguments)
    {
        bool reached = false;

        object? result = await new DashboardRequestValidationFilter()
            .InvokeAsync(
                new TestInvocationContext(arguments),
                _ =>
                {
                    reached = true;
                    return ValueTask.FromResult<object?>(Results.Ok());
                }
            )
            .ConfigureAwait(false);

        result.Should().NotBeNull();

        return reached;
    }

    private sealed class TestInvocationContext(object?[] arguments)
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = new DefaultHttpContext();

        public override IList<object?> Arguments { get; } = arguments;

        public override T GetArgument<T>(int index) => (T)Arguments[index]!;
    }
}
