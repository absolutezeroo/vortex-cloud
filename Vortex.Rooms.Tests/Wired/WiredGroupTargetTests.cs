using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The group-aware wired boxes carry their whole configuration in the string param: empty means the
/// form's first option ("Current group"), anything else is a guild id in decimal. Reading the empty
/// string as "no group" instead of "this room's group" would silently break every guild-base box.
/// </summary>
public sealed class WiredGroupTargetTests
{
    [Fact]
    public void EmptyParam_MeansTheRoomsOwnGuild()
    {
        WiredGroupTarget.Resolve(string.Empty, 42).Should().Be(42);
        WiredGroupTarget.Resolve(null, 42).Should().Be(42);
        WiredGroupTarget.Resolve("   ", 42).Should().Be(42);
    }

    [Fact]
    public void EmptyParam_InARoomWithNoGuild_ResolvesToNothing()
    {
        WiredGroupTarget.Resolve(string.Empty, null).Should().BeNull();
        WiredGroupTarget.Resolve(string.Empty, 0).Should().BeNull();
    }

    [Fact]
    public void ExplicitId_WinsOverTheRoomsGuild()
    {
        WiredGroupTarget.Resolve("7", 42).Should().Be(7);
        WiredGroupTarget.Resolve(" 7 ", 42).Should().Be(7);
        WiredGroupTarget.Resolve("7", null).Should().Be(7);
    }

    [Fact]
    public void UnusableParam_ResolvesToNothing_RatherThanTheRoomsGuild()
    {
        // Falling back to the room's guild here would quietly answer a different question than the
        // one the box was configured with.
        WiredGroupTarget.Resolve("abc", 42).Should().BeNull();
        WiredGroupTarget.Resolve("0", 42).Should().BeNull();
        WiredGroupTarget.Resolve("-3", 42).Should().BeNull();
    }
}
