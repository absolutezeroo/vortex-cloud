using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Logging;
using Vortex.Pipeline;
using Vortex.Primitives;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Orleans.Snapshots.Room;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Messages.Registry;

public sealed class MessageRegistry(
    IServiceProvider sp,
    IErrorGroupingSink errorGroupingSink,
    IVortexContextAccessor contextAccessor,
    ILogger<MessageRegistry> logger
)
    : EnvelopeHost<IMessageEvent, ISessionContext, MessageContext>(
        sp,
        CreateOptions(sp, errorGroupingSink, contextAccessor, logger)
    )
{
    private static EnvelopeHostOptions<
        IMessageEvent,
        ISessionContext,
        MessageContext
    > CreateOptions(
        IServiceProvider serviceProvider,
        IErrorGroupingSink errorGroupingSink,
        IVortexContextAccessor contextAccessor,
        ILogger<MessageRegistry> logger
    )
    {
        return new EnvelopeHostOptions<IMessageEvent, ISessionContext, MessageContext>
        {
            CreateContextAsync = async (env, data) =>
            {
                if (data is null)
                {
                    throw new VortexException(VortexErrorCodeEnum.InvalidSession);
                }

                IGrainFactory grainFactory = serviceProvider.GetRequiredService<IGrainFactory>();
                ISessionGateway sessionGateway =
                    serviceProvider.GetRequiredService<ISessionGateway>();
                PlayerId playerId = sessionGateway.GetPlayerId(data.SessionKey);
                int roomId = -1;

                if (playerId > 0)
                {
                    IPlayerPresenceGrain playerPresence = grainFactory.GetPlayerPresenceGrain(
                        playerId
                    );
                    RoomPointerSnapshot activeRoom = await playerPresence
                        .GetActiveRoomAsync()
                        .ConfigureAwait(false);

                    roomId = activeRoom.RoomId;
                }

                return new(data, playerId, roomId);
            },
            EnableInheritanceDispatch = true,
            HandlerMode = HandlerExecutionMode.Parallel,
            MaxHandlerDegreeOfParallelism = null,
            OnHandlerActivationError = (ex, env) =>
                ReportError(
                    ex,
                    "message-registry.activation",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnHandlerInvokeError = (ex, env) =>
                ReportError(
                    ex,
                    "message-registry.invoke",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnBehaviorActivationError = (ex, env) =>
                ReportError(
                    ex,
                    "message-registry.behavior-activation",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnBehaviorInvokeError = (ex, env) =>
                ReportError(
                    ex,
                    "message-registry.behavior-invoke",
                    env,
                    errorGroupingSink,
                    contextAccessor,
                    logger
                ),
            OnNoHandlerRegistered = env =>
                logger.LogWarning(
                    "No handler registered for incoming message {MessageType}",
                    env.GetType().Name
                ),
        };
    }

    private static void ReportError(
        Exception ex,
        string source,
        object env,
        IErrorGroupingSink errorGroupingSink,
        IVortexContextAccessor contextAccessor,
        ILogger logger
    )
    {
        IVortexContext? context = contextAccessor.Current;

        try
        {
            errorGroupingSink.Record(
                ex,
                source,
                env.GetType().Name,
                context?.PlayerId,
                context?.RoomId,
                context?.CorrelationId.Value,
                context?.SessionKey
            );

            logger.LogWarning(
                ex,
                "Message registry pipeline failure: {Source}/{Operation}",
                source,
                env.GetType().Name
            );
        }
        catch (Exception reportEx)
        {
            logger.LogError(
                reportEx,
                "Message registry failed to report a pipeline error for {Operation}",
                env.GetType().Name
            );
        }
    }
}
