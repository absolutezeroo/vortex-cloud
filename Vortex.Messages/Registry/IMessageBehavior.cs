using Vortex.Pipeline.Registry;
using Vortex.Primitives.Networking;

namespace Vortex.Messages.Registry;

public interface IMessageBehavior<in T> : IBehavior<T, MessageContext>
    where T : IMessageEvent;
