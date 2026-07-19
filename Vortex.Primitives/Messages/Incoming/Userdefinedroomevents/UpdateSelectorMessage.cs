using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public record UpdateSelectorMessage : UpdateWiredMessage, IMessageEvent { }
