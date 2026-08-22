using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using Vortex.Messages;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Packets;

namespace Vortex.Networking.Package;

public sealed class PackageHandler(
    IRevisionManager revisionManager,
    MessageSystem messageSystem,
    ILogger<PackageHandler> logger,
    IVortexContextAccessor contextAccessor,
    IErrorGroupingSink errorSink
) : IPackageHandler<IClientPacket>
{
    private readonly IVortexContextAccessor _contextAccessor = contextAccessor;
    private readonly IErrorGroupingSink _errorSink = errorSink;
    private readonly ILogger<PackageHandler> _logger = logger;
    private readonly MessageSystem _messageSystem = messageSystem;
    private readonly IRevisionManager _revisionManager = revisionManager;

    public ValueTask Handle(IAppSession session, IClientPacket packet, CancellationToken ct)
    {
        return HandleCoreAsync((ISessionContext)session, packet, ct);
    }

    /// <summary>
    ///     Direct handler for transports whose session context is managed outside SuperSocket.
    /// </summary>
    public ValueTask HandleAsync(ISessionContext ctx, IClientPacket packet, CancellationToken ct)
    {
        return HandleCoreAsync(ctx, packet, ct);
    }

    public async ValueTask HandleCoreAsync(
        ISessionContext ctx,
        IClientPacket packet,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(packet);

        try
        {
            IRevision revision =
                _revisionManager.GetRevision(ctx.RevisionId)
                ?? throw new InvalidOperationException(
                    $"No revision registered for revision id '{ctx.RevisionId}'."
                );

            if (revision.Parsers.TryGetValue(packet.Header, out IParser? parser))
            {
                IMessageEvent message = parser.Parse(packet);

                _logger.LogDebug("Incoming {MessageType}", message.GetType().Name);

                await _messageSystem.PublishAsync(message, ctx, ct).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "Incoming Unknown {Header} for {SessionKey}",
                    packet.Header,
                    ctx.SessionKey
                );
            }
        }
        catch (Exception ex)
        {
            // The bytes, not just the stack. A parser that disagrees with the client cannot be
            // diagnosed from the exception -- the position is where the two stopped agreeing, and
            // without the payload the next step is guesswork.
            _logger.LogError(
                ex,
                "Failed to process packet {Packet} for session {SessionKey} at byte {Position} of {Length} ({Remaining} left): {Payload}",
                packet.Header,
                ctx.SessionKey,
                packet.Position,
                packet.Position + packet.Remaining,
                packet.Remaining,
                packet.ToHexDump()
            );

            IVortexContext? context = _contextAccessor.Current;

            _errorSink.Record(
                ex,
                "package-handler",
                $"packet:{packet.Header}",
                context?.PlayerId,
                context?.RoomId,
                context?.CorrelationId.Value,
                context?.SessionKey,
                ctx.RemoteIpAddress
            );
        }
    }
}
