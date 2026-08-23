using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;

[GenerateSerializer, Immutable]
public record UpdateAddonMessage : UpdateWiredMessage, IMessageEvent { }
