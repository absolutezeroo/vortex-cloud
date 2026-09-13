using System;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Turns a chat line into the facial expression the speaker's avatar plays while saying it.
/// </summary>
/// <remarks>
/// The client never derives this itself: <c>room/_SafeCls_1984.as:1502</c> plays exactly the gesture
/// the chat packet carried, so a server that always sends <see cref="AvatarGestureType.None"/> — as
/// this one did — gives every room a cast of blank faces. The smiley table below is the reference
/// emulator's (<c>RoomChatMessage.checkEmotion</c>), which maps onto the client's gesture ids 1-4;
/// official behaviour is unknown and recorded as such in the Chat spec.
/// </remarks>
internal static class ChatGestures
{
    public static AvatarGestureType FromText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return AvatarGestureType.None;
        }

        if (Contains(text, ":)") || Contains(text, ":-)") || Contains(text, ":]"))
        {
            return AvatarGestureType.Smile;
        }

        // Before the sad test on purpose: ">:(" ends in ":(" and is the angry face, not a sad one.
        if (Contains(text, ":@") || Contains(text, ">:("))
        {
            return AvatarGestureType.Angry;
        }

        if (
            Contains(text, ":o")
            || Contains(text, ":0")
            || Contains(text, "o.o")
            || Contains(text, "0.o")
            || Contains(text, "o.0")
        )
        {
            return AvatarGestureType.Surprised;
        }

        if (Contains(text, ":(") || Contains(text, ":-(") || Contains(text, ":["))
        {
            return AvatarGestureType.Sad;
        }

        return AvatarGestureType.None;
    }

    private static bool Contains(string text, string token) =>
        text.Contains(token, StringComparison.OrdinalIgnoreCase);
}
