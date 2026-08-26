using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Permissions;
using Xunit;

namespace Vortex.Dashboard.Tests.Hosting;

/// <summary>
/// The operator security context: what an operation reads when it needs to know who is asking, as
/// opposed to what it was told.
/// </summary>
/// <remarks>
/// <para>
/// The frozen note's §5 boundary. A privileged operation may not depend on a bare <c>string actor</c>
/// as its only security context — that string is an argument every method forwards and nothing
/// checks, so an audit trail built on it records what it was told rather than what happened.
/// </para>
/// <para>
/// Ambient rather than threaded, so the rules that matter are the ones a scope has to keep: it must
/// not leak past the request, it must nest, and it must be absent when there is no request rather
/// than stale from the last one. A leaked context is the worst possible failure here — the next
/// request would be audited as the previous operator.
/// </para>
/// </remarks>
public sealed class ActorSecurityContextTests
{
    [Fact]
    public void ThereIsNoOperatorOutsideARequest()
    {
        ActorSecurityContext.Current.Should().BeNull();
    }

    [Fact]
    public void AScopePublishesItsOperatorAndTakesItBack()
    {
        using (IDisposable _ = ActorSecurityContext.Enter(Operator(1, "first@vortex.test")))
        {
            ActorSecurityContext.Current!.Email.Should().Be("first@vortex.test");
            ActorSecurityContext.Current.AccountId.Should().Be(1);
        }

        ActorSecurityContext.Current.Should().BeNull("the request is over");
    }

    /// <summary>
    /// Nesting restores the outer operator rather than clearing. Nothing nests today, and a scope
    /// that cleared on exit would work right up until something did.
    /// </summary>
    [Fact]
    public void ANestedScopeRestoresTheOneAroundIt()
    {
        using IDisposable outer = ActorSecurityContext.Enter(Operator(1, "outer@vortex.test"));

        using (IDisposable _ = ActorSecurityContext.Enter(Operator(2, "inner@vortex.test")))
        {
            ActorSecurityContext.Current!.Email.Should().Be("inner@vortex.test");
        }

        ActorSecurityContext.Current!.Email.Should().Be("outer@vortex.test");
    }

    /// <summary>
    /// The one that matters. Two requests served concurrently must not see each other's operator —
    /// an ambient value that leaked across them would audit one operator's writes under the other's
    /// name, and the audit trail is the thing nobody can reconstruct afterwards.
    /// </summary>
    [Fact]
    public async Task ConcurrentRequestsDoNotSeeEachOthersOperator()
    {
        async Task<string> ServeAsync(int accountId, string email)
        {
            using IDisposable _ = ActorSecurityContext.Enter(Operator(accountId, email));

            // Yield repeatedly, so the two flows genuinely interleave rather than running one after
            // the other and passing by accident.
            for (int i = 0; i < 20; i++)
            {
                await Task.Yield();
            }

            return ActorSecurityContext.Current!.Email;
        }

        Task<string> first = ServeAsync(1, "alice@vortex.test");
        Task<string> second = ServeAsync(2, "bob@vortex.test");

        (await first).Should().Be("alice@vortex.test");
        (await second).Should().Be("bob@vortex.test");
        ActorSecurityContext.Current.Should().BeNull();
    }

    /// <summary>
    /// Capabilities come from the context, not from a claim the caller repeats. This is what lets an
    /// operation make a decision of its own rather than trusting its arguments.
    /// </summary>
    [Fact]
    public void CapabilitiesAreReadFromTheContext()
    {
        using IDisposable _ = ActorSecurityContext.Enter(
            Operator(1, "ops@vortex.test", Capabilities.Dashboard.OpsGrantCurrency)
        );

        ActorSecurityContext
            .Current!.Has(Capabilities.Dashboard.OpsGrantCurrency)
            .Should()
            .BeTrue();
        ActorSecurityContext.Current.Has(Capabilities.Dashboard.OpsStaffManage).Should().BeFalse();
    }

    private static ActorSecurityContext Operator(
        int accountId,
        string email,
        params string[] capabilities
    ) =>
        new()
        {
            AccountId = accountId,
            Email = email,
            SessionId = $"session-{accountId}",
            Permissions = new PermissionSet([], capabilities),
            SteppedUpAtUtc = null,
            CorrelationId = CorrelationId.New(),
        };
}
