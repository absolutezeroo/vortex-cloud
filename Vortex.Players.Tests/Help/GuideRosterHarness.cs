using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Events;
using Vortex.Social.Grains;
using Vortex.Tests.Support;

namespace Vortex.Players.Tests.Help;

/// <summary>
/// Builds the guide roster outside a silo.
/// </summary>
/// <remarks>
/// The grain sends its own packets now — a review can end because nobody answered, which has no
/// handler behind it — so the factory has to answer <c>GetGrain&lt;T&gt;()</c> with something rather
/// than null. Every grain it hands back is a stub whose calls complete and do nothing, which is
/// exactly what these tests want: they assert on the decisions, not on what went out.
/// </remarks>
internal static class GuideRosterHarness
{
    public static GuideDirectoryGrain New() =>
        GrainActivationContext.CreateWithIntegerKey<GuideDirectoryGrain>(
            0,
            StubFactory(),
            // The roster raises request/session records for the audit trail. These tests are about
            // the roster's own bookkeeping, so the publisher only has to be awaitable.
            FakeProxy.Create<IEventPublisher>(_ => Task.CompletedTask)
        );

    private static IGrainFactory StubFactory() =>
        FakeProxy.Create<IGrainFactory>(call =>
            call.Method.Name == "GetGrain" && call.Method.IsGenericMethod
                ? FakeProxy.CreateFor(call.Method.GetGenericArguments()[0], _ => null)
                : null
        );
}
