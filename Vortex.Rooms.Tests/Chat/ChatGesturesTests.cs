using FluentAssertions;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Chat;

/// <summary>
/// The smiley table that gives an avatar its face while it speaks. The client plays whatever
/// gesture id the chat packet carried and derives nothing itself, so every case here is the
/// difference between an expression and a blank stare.
/// </summary>
public sealed class ChatGesturesTests
{
    [Theory]
    [InlineData("hello :)", AvatarGestureType.Smile)]
    [InlineData("hello :-)", AvatarGestureType.Smile)]
    [InlineData("hello :]", AvatarGestureType.Smile)]
    [InlineData("what :@", AvatarGestureType.Angry)]
    [InlineData("oh :o", AvatarGestureType.Surprised)]
    [InlineData("oh :0", AvatarGestureType.Surprised)]
    [InlineData("o.O really", AvatarGestureType.Surprised)]
    [InlineData("bye :(", AvatarGestureType.Sad)]
    [InlineData("bye :-(", AvatarGestureType.Sad)]
    [InlineData("bye :[", AvatarGestureType.Sad)]
    [InlineData("just talking", AvatarGestureType.None)]
    [InlineData("", AvatarGestureType.None)]
    [InlineData(null, AvatarGestureType.None)]
    public void MapsSmileysToTheClientsGestureIds(string? text, AvatarGestureType expected) =>
        ChatGestures.FromText(text).Should().Be(expected);

    /// <summary>
    /// ">:(" ends in the sad smiley and is the angry one. Test the order, not just the table.
    /// </summary>
    [Fact]
    public void AngryWinsOverTheSadSmileyItContains() =>
        ChatGestures.FromText("grr >:(").Should().Be(AvatarGestureType.Angry);
}
