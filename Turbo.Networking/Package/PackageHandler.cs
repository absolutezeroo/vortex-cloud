using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using Turbo.Messages;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Networking.Revisions;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Packets;

namespace Turbo.Networking.Package;

public sealed class PackageHandler(
    IRevisionManager revisionManager,
    MessageSystem messageSystem,
    ILogger<PackageHandler> logger,
    ITurboContextAccessor? contextAccessor = null,
    IErrorGroupingSink? errorSink = null
) : IPackageHandler<IClientPacket>
{
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly MessageSystem _messageSystem = messageSystem;
    private readonly IErrorGroupingSink? _errorSink = errorSink;
    private readonly ITurboContextAccessor? _contextAccessor = contextAccessor;
    private readonly ILogger<PackageHandler> _logger = logger;

    public async ValueTask Handle(IAppSession session, IClientPacket packet, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(packet);

        ISessionContext ctx = (ISessionContext)session;

        try
        {
            IRevision revision =
                _revisionManager.GetRevision(ctx.RevisionId)
                ?? throw new ArgumentNullException("No revision set");

            if (revision.Parsers.TryGetValue(packet.Header, out IParser? parser))
            {
                IMessageEvent message = parser.Parse(packet);

                _logger.LogDebug("Incoming {Message}", message);

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
            _logger.LogError(
                ex,
                "Failed to process packet {Packet} for session {SessionKey}",
                packet.Header,
                ctx.SessionKey
            );

            ITurboContext? context = _contextAccessor?.Current;

            if (_errorSink is not null)
            {
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
}
