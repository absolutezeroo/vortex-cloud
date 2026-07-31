using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Vortex.Plugins.Tests;

/// <summary>
/// The rollback stack is what turns a half-finished plugin activation back into "nothing happened".
/// If it runs steps in the wrong order, or gives up after the first compensation throws, the host is
/// left holding a started hosted service or an undisposed service provider — which in turn pins the
/// plugin's <c>AssemblyLoadContext</c> in memory forever.
/// </summary>
public sealed class ActivationRollbackTests
{
    [Fact]
    public async Task Compensations_RunInReverseOrder()
    {
        List<string> order = [];
        ActivationRollback rollback = new(NullLogger.Instance, "plugin");

        rollback.Push("first", () => order.Add("first"));
        rollback.Push("second", () => order.Add("second"));
        rollback.Push("third", () => order.Add("third"));

        await rollback.UnwindAsync();

        order.Should().Equal("third", "second", "first");
    }

    [Fact]
    public async Task AThrowingCompensation_DoesNotStopTheRest()
    {
        List<string> order = [];
        ActivationRollback rollback = new(NullLogger.Instance, "plugin");

        rollback.Push("outer", () => order.Add("outer"));
        rollback.Push("broken", () => throw new InvalidOperationException("cleanup blew up"));
        rollback.Push("inner", () => order.Add("inner"));

        // Cleanup is best effort: UnwindAsync itself must never throw.
        await rollback.UnwindAsync();

        order.Should().Equal("inner", "outer");
    }

    [Fact]
    public async Task Commit_DiscardsTheCompensationsWithoutRunningThem()
    {
        List<string> order = [];
        ActivationRollback rollback = new(NullLogger.Instance, "plugin");

        rollback.Push("undo", () => order.Add("undo"));
        rollback.Commit();

        await rollback.UnwindAsync();

        order.Should().BeEmpty("a committed activation owns its own teardown");
    }

    [Fact]
    public async Task Unwinding_IsIdempotent()
    {
        List<string> order = [];
        ActivationRollback rollback = new(NullLogger.Instance, "plugin");

        rollback.Push("undo", () => order.Add("undo"));

        await rollback.UnwindAsync();
        await rollback.UnwindAsync();

        order.Should().Equal("undo");
    }
}
