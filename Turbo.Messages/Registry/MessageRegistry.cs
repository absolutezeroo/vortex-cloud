using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Logging;
using Turbo.Pipeline;
using Turbo.Primitives;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Messages.Registry;

public sealed class MessageRegistry(
    IServiceProvider sp,
    IErrorGroupingSink errorGroupingSink,
    ITurboContextAccessor contextAccessor,
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
        ITurboContextAccessor contextAccessor,
        ILogger<MessageRegistry> logger
    )
    {
        return new EnvelopeHostOptions<IMessageEvent, ISessionContext, MessageContext>
        {
            CreateContextAsync = async (env, data) =>
            {
                if (data is null)
                {
                    throw new TurboException(TurboErrorCodeEnum.InvalidSession);
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
        };
    }

    private static void ReportError(
        Exception ex,
        string source,
        object env,
        IErrorGroupingSink errorGroupingSink,
        ITurboContextAccessor contextAccessor,
        ILogger logger
    )
    {
        ITurboContext? context = contextAccessor.Current;

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
