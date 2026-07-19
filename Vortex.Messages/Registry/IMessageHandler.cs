using Vortex.Pipeline.Registry;
using Vortex.Primitives.Networking;

namespace Vortex.Messages.Registry;

public interface IMessageHandler<in T> : IHandler<T, MessageContext>
    where T : IMessageEvent;
