using Vortex.Messages.Registry;
using Vortex.Pipeline;
using Vortex.Primitives.Networking;

namespace Vortex.Messages;

internal sealed class MessageFeatureProcessor(
    MessageRegistry registry,
    EnvelopeInvokerFactory<MessageContext> invokerFactory
)
    : EnvelopeFeatureProcessor<IMessageEvent, ISessionContext, MessageContext>(
        registry,
        invokerFactory,
        typeof(IMessageHandler<>),
        typeof(IMessageBehavior<>)
    ) { }
