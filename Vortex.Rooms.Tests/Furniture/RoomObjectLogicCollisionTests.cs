using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Logging;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Rooms.Providers;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Two implementations claiming one logic name is a collision, not an override.
/// </summary>
/// <remarks>
/// The registry used to take the last registration and forget the first (<c>_logics[key] = reg</c>),
/// and its disposable removed the key only when the registration being removed was still the current
/// one. So a plugin registering a name the core already owned took the furni over silently, and
/// unloading that plugin left the core's furni resolving to the family default: it behaved like a
/// plain item, wrote a warning nobody was reading, and stayed that way until the next restart.
/// <para>
/// The hot-unload test below is the one that used to fail.
/// </para>
/// </remarks>
public sealed class RoomObjectLogicCollisionTests
{
    private const string LOGIC_NAME = "collision_test_logic";

    [Fact]
    public void ASecondRegistrationOfTheSameNameAndFamily_IsRefused()
    {
        RoomObjectLogicProvider provider = Build();

        using IDisposable first = Register<FirstLogic>(provider);

        Action second = () => Register<SecondLogic>(provider);

        second
            .Should()
            .Throw<VortexException>(
                "the incoming registration is refused rather than silently replacing the one there"
            );
    }

    /// <summary>The one that used to fail: unloading a colliding plugin left the core furni broken.</summary>
    [Fact]
    public void AFailedRegistrationBeingDisposed_LeavesTheFirstOneWorking()
    {
        RoomObjectLogicProvider provider = Build();

        using IDisposable core = Register<FirstLogic>(provider);

        try
        {
            Register<SecondLogic>(provider);
        }
        catch (VortexException)
        {
            // What a plugin loader does next: give up on the plugin and unload it. Whatever it
            // disposes must not take the core's registration with it.
        }

        provider
            .CreateLogicInstance(LOGIC_NAME, FloorContext())
            .Should()
            .BeOfType<FirstLogic>("the core registration is still the one that was there");
    }

    [Fact]
    public void ARegistrationDisposedByItsOwner_IsGoneAndFallsBack()
    {
        RoomObjectLogicProvider provider = Build();

        using IDisposable fallback = Register<DefaultFloorLogic>(provider, "default_floor");
        IDisposable registration = Register<FirstLogic>(provider);

        registration.Dispose();

        provider
            .CreateLogicInstance(LOGIC_NAME, FloorContext())
            .Should()
            .BeOfType<DefaultFloorLogic>();
    }

    /// <summary>
    /// One assembly processed twice registers the same class twice, which is a host wiring question
    /// and not a reason to refuse to start. Only two <em>different</em> implementations are a
    /// collision.
    /// </summary>
    [Fact]
    public void TheSameImplementationRegisteredTwice_IsNotACollision()
    {
        RoomObjectLogicProvider provider = Build();

        using IDisposable first = Register<FirstLogic>(provider);
        IDisposable second = Register<FirstLogic>(provider);

        // And the second disposable is inert: disposing it must not take the live registration away.
        second.Dispose();

        provider.CreateLogicInstance(LOGIC_NAME, FloorContext()).Should().BeOfType<FirstLogic>();
    }

    private static RoomObjectLogicProvider Build() =>
        new(
            FakeProxy.Create<IServiceProvider>(_ => null),
            FakeProxy.Create<IVortexMetrics>(_ => null),
            NullLogger<RoomObjectLogicProvider>.Instance
        );

    /// <summary>
    /// Registers a stub under a name. The stubs implement neither family interface, so they all key
    /// under <c>Any</c> — which is what makes two of them under one name a collision. Family keying
    /// has its own suite (<see cref="RoomObjectLogicFamilyTests"/>).
    /// </summary>
    private static IDisposable Register<TLogic>(
        RoomObjectLogicProvider provider,
        string name = LOGIC_NAME
    )
        where TLogic : IRoomObjectLogic, new() =>
        provider.RegisterLogic(
            name,
            typeof(TLogic),
            FakeProxy.Create<IServiceProvider>(_ => null),
            (_, _) => new TLogic()
        );

    private static IRoomObjectContext FloorContext() =>
        FakeProxy.Create<IRoomFloorItemContext>(_ => null);

    private sealed class FirstLogic : StubLogic { }

    private sealed class SecondLogic : StubLogic { }

    private sealed class DefaultFloorLogic : StubLogic { }

    private abstract class StubLogic : IRoomObjectLogic
    {
        public IRoomObjectContext Context { get; set; } = null!;

        public Task OnAttachAsync(CancellationToken ct) => Task.CompletedTask;

        public Task OnDetachAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
