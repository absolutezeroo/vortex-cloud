using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

/// <summary>
/// Client asks for one <c>furniture_definitions</c> row, to populate the definition editor.
/// Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
public record VortexGetFurniDefinitionMessage : IMessageEvent
{
    public required int DefinitionId { get; init; }
}
