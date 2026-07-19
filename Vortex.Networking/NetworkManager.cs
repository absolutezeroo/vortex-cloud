using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Host;
using SuperSocket.Server.Host;
using SuperSocket.WebSocket.Server;
using Vortex.Messages;
using Vortex.Networking.Configuration;
using Vortex.Networking.Extensions;
using Vortex.Networking.Package;
using Vortex.Networking.Session;
using Vortex.Networking.Tcp;
using Vortex.Networking.Ws;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;
using Vortex.Primitives.Packets;

namespace Vortex.Networking;

public sealed class NetworkManager(
    IOptions<NetworkingConfig> config,
    ISessionGateway sessionGateway,
    IRevisionManager revisionManager,
    MessageSystem messageSystem,
    ILoggerFactory loggerFactory,
    IGrainFactory grainFactory
) : INetworkManager
{
    private readonly NetworkingConfig _config = config.Value;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<NetworkManager> _logger = loggerFactory.CreateLogger<NetworkManager>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly MessageSystem _messageSystem = messageSystem;
    private readonly IPackageEncoder<OutgoingPackage> _packageEncoder = new PackageEncoder(
        revisionManager,
        loggerFactory.CreateLogger<PackageEncoder>()
    );
    private readonly IRevisionManager _revisionManager = revisionManager;
    private readonly ISessionGateway _sessionGateway = sessionGateway;

    private readonly Lock _tcpGate = new();
    private readonly Lock _wsGate = new();

    private readonly ConcurrentDictionary<string, WebSocketSessionContext> _wsSessions = new();

    private IHost? _tcpHost;
    private IHost? _wsHost;

    public async Task StartAsync(CancellationToken ct)
    {
        bool needTcpStart = false;
        bool needsWsStart = false;

        lock (_tcpGate)
        {
            if (_tcpHost is null)
            {
                CreateTcpSocket();
                needTcpStart = true;
            }
        }

        lock (_wsGate)
        {
            if (_wsHost is null)
            {
                CreateWsSocket();
                needsWsStart = true;
            }
        }

        if (needTcpStart && _tcpHost is not null)
        {
            await _tcpHost.StartAsync(ct).ConfigureAwait(false);
        }

        if (needsWsStart && _wsHost is not null)
        {
            await _wsHost.StartAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        IHost? tcpHostToStop = null;
        IHost? wsHostToStop = null;

        lock (_tcpGate)
        {
            tcpHostToStop = _tcpHost;
            _tcpHost = null;
        }

        lock (_wsGate)
        {
            wsHostToStop = _wsHost;
            _wsHost = null;
        }

        Task stopTcp = tcpHostToStop?.StopAsync(TimeSpan.FromSeconds(5)) ?? Task.CompletedTask;
        Task stopWs = wsHostToStop?.StopAsync(TimeSpan.FromSeconds(5)) ?? Task.CompletedTask;

        await Task.WhenAll(stopTcp, stopWs).ConfigureAwait(false);
    }

    private void CreateTcpSocket()
    {
        ISuperSocketHostBuilder<IClientPacket>? builder =
            SuperSocketHostBuilder.Create<IClientPacket>();

        builder.ConfigureServerOptions((ctx, config) => config.GetSection("TcpServer"));
        builder.ConfigureLogging((ctx, logging) => logging.ClearProviders());
        builder.ConfigureServices((ctx, services) => ConfigureCommonServices(services));
        builder.UseSession<SessionContext>();
        builder.UsePipelineFilter<TcpFilter>();
        builder.UseSessionGateway();
        //builder.UsePingPong();

        _tcpHost = builder.Build();
    }

    private void CreateWsSocket()
    {
        WebSocketHostBuilder? builder = WebSocketHostBuilder.Create();

        builder.ConfigureServerOptions((ctx, config) => config.GetSection("WebSocketServer"));
        builder.ConfigureLogging((ctx, logging) => logging.ClearProviders());
        builder.ConfigureServices((ctx, services) => ConfigureCommonServices(services));

        ClientPacketDecoder decoder = new(_config.MaxPacketBodyBytes);
        PackageHandler packageHandler = new(
            _revisionManager,
            _messageSystem,
            _loggerFactory.CreateLogger<PackageHandler>()
        );
        WsPackageHandler wsPackageHandler = new(
            decoder,
            packageHandler,
            _loggerFactory.CreateLogger<WsPackageHandler>()
        );

        builder.UseSessionHandler(
            async session =>
            {
                WebSocketSession wsSession = (WebSocketSession)session;
                WebSocketSessionContext context = new(
                    wsSession,
                    _packageEncoder,
                    _loggerFactory.CreateLogger<WebSocketSessionContext>()
                );

                context.SetRevisionId(_revisionManager.DefaultRevisionId);
                _wsSessions[session.SessionID] = context;

                await _sessionGateway
                    .AddSessionAsync(context.SessionKey, context)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "WebSocket session connected: {SessionId}",
                    session.SessionID
                );
            },
            async (session, e) =>
            {
                if (_wsSessions.TryRemove(session.SessionID, out WebSocketSessionContext? context))
                {
                    await _sessionGateway
                        .RemoveSessionAsync(context.SessionKey, CancellationToken.None)
                        .ConfigureAwait(false);
                    context.Dispose();
                }

                _logger.LogInformation(
                    "WebSocket session disconnected: {SessionId}",
                    session.SessionID
                );
            }
        );

        builder.UseWebSocketMessageHandler(
            async (session, message) =>
            {
                if (
                    !_wsSessions.TryGetValue(
                        session.SessionID,
                        out WebSocketSessionContext? context
                    )
                )
                {
                    return;
                }

                await wsPackageHandler
                    .HandleManualAsync(context, message, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        );

        _wsHost = builder.Build();
    }

    private void ConfigureCommonServices(IServiceCollection services)
    {
        services.AddSingleton(_sessionGateway);
        services.AddSingleton(_revisionManager);
        services.AddSingleton(_messageSystem);
        services.AddSingleton(_loggerFactory);
        services.AddSingleton(_grainFactory);
        services.AddSingleton<IPackageEncoder<OutgoingPackage>, PackageEncoder>();
        services.AddSingleton<IPackageHandler<IClientPacket>, PackageHandler>();
        services.AddSingleton<IClientPacketDecoder>(_ => new ClientPacketDecoder(
            _config.MaxPacketBodyBytes
        ));
    }
}
