using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>
/// "Show me my sanctions." Body-less: the client's composer returns an empty message array, so
/// there is nothing to read — a field here used to make the parser consume bytes that were never
/// sent.
/// </summary>
public record GetCfhStatusMessage : IMessageEvent { }
