using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>
/// A player asking a guide for help.
/// </summary>
/// <remarks>
/// The type is the client's own entry point: <c>createHelpRequest(0)</c> and <c>(2)</c> are tour
/// requests, <c>(1)</c> is a help request. Types 0 and 2 travel with a canned description the client
/// fills in from its own localization; type 1 carries what the player typed.
/// </remarks>
public record GuideSessionCreateMessage : IMessageEvent
{
    public required int HelpRequestType { get; init; }
    public required string Description { get; init; }
}
