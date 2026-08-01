namespace Vortex.Primitives.Players.Enums;

/// <summary>
/// The onboarding steps the server can ask the client to run, sent in
/// <see cref="Messages.Outgoing.Handshake.AuthenticationOKMessage.SuggestedLoginActions"/>.
/// </summary>
/// <remarks>
/// Values are the client's, from
/// <c>WIN63-202607011411-782849652/src/com/sulake/habbo/friendbar/onBoardingHc/OnBoardingHcFlow.as</c>
/// (<c>AVATAR_NAME_CHANGE = 0</c>, <c>NEW_ROOM_SELECT = 1</c>). Anything else is ignored by the
/// client: <c>HabboLandingView.isOnboardingRequired()</c> only tests for these two.
/// </remarks>
public static class SuggestedLoginAction
{
    public const short AvatarNameChange = 0;
    public const short NewRoomSelect = 1;
}
