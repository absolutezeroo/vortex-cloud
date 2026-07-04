using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Turbo.Logging.Extensions;
using Turbo.Players.Configuration;
using Turbo.Players.Grains.Modules;
using Turbo.Primitives.Events;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Orleans.Observers;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Rooms.Snapshots;

namespace Turbo.Players.Grains;

internal sealed partial class PlayerPresenceGrain
    : Grain,
        IPlayerPresenceGrain,
        IAsyncObserver<RoomOutbound>
{
    internal readonly IEventPublisher _events;
    internal readonly IGrainFactory _grainFactory;
    private readonly ILogger<PlayerPresenceGrain> _logger;
    private readonly PlayerPresenceConfig _config;

    private readonly PlayerInventoryModule _inventoryModule;

    private readonly Queue<IComposer> _outgoingQueue = new();
    internal readonly PlayerPresenceLiveState _state;
    private readonly PlayerWalletModule _walletModule;
    private bool _isProcessingQueue;
    private StreamSubscriptionHandle<RoomOutbound>? _roomOutboundSub;

    private ISessionContextObserver? _sessionObserver;

    public PlayerPresenceGrain(
        IGrainFactory grainFactory,
        IEventPublisher events,
        ILogger<PlayerPresenceGrain> logger,
        IOptions<PlayerPresenceConfig> config
    )
    {
        _grainFactory = grainFactory;
        _events = events;
        _logger = logger;
        _config = config.Value;

        _state = new PlayerPresenceLiveState();
        _inventoryModule = new PlayerInventoryModule(this);
        _walletModule = new PlayerWalletModule(this);
    }

    public Task OnNextAsync(RoomOutbound item, StreamSequenceToken? token = null)
    {
        if (
            _sessionObserver is null
            || (
                item.ExcludedPlayerIds is not null
                && item.ExcludedPlayerIds.Contains((int)this.GetPrimaryKeyLong())
            )
        )
        {
            return Task.CompletedTask;
        }

        return SendComposerAsync(item.Composer);
    }

    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        _logger.LogWarning(
            ex,
            "Room outbound stream error for player {PlayerId}",
            this.GetPrimaryKeyLong()
        );

        return Task.CompletedTask;
    }

    public Task RegisterSessionObserverAsync(ISessionContextObserver observer)
    {
        _sessionObserver = observer;

        return Task.CompletedTask;
    }

    public async Task UnregisterSessionObserverAsync(CancellationToken ct)
    {
        await ClearActiveRoomAsync(ct);

        _sessionObserver = null;
    }

    public Task<bool> IsOnlineAsync(CancellationToken ct)
    {
        return Task.FromResult(_sessionObserver is not null);
    }

    public Task SendComposerAsync(IComposer composer)
    {
        if (composer is not null)
        {
            EnqueueOutgoing(composer);

            LogAndForget(ProcessOutgoingQueueAsync());
        }

        return Task.CompletedTask;
    }

    public Task SendComposerAsync(params IComposer[] composers)
    {
        if (composers.Length > 0)
        {
            foreach (IComposer composer in composers)
            {
                EnqueueOutgoing(composer);
            }

            LogAndForget(ProcessOutgoingQueueAsync());
        }

        return Task.CompletedTask;
    }

    private void EnqueueOutgoing(IComposer composer)
    {
        _outgoingQueue.Enqueue(composer);

        while (_outgoingQueue.Count > _config.MaxOutgoingQueueSize)
        {
            _outgoingQueue.Dequeue();

            _logger.LogWarning(
                "Outgoing composer queue for player {PlayerId} exceeded {MaxOutgoingQueueSize}; dropping oldest composer",
                this.GetPrimaryKeyLong(),
                _config.MaxOutgoingQueueSize
            );
        }
    }

    public override Task OnActivateAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _outgoingQueue.Clear();

        if (_roomOutboundSub is not null)
        {
            try
            {
                await _roomOutboundSub.UnsubscribeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to unsubscribe room outbound stream for player {PlayerId} on deactivation",
                    this.GetPrimaryKeyLong()
                );
            }

            _roomOutboundSub = null;
        }
    }

    private async Task ProcessOutgoingQueueAsync()
    {
        if (_isProcessingQueue)
        {
            return;
        }

        _isProcessingQueue = true;

        await Task.Yield();

        if (_sessionObserver is not null)
        {
            while (_outgoingQueue.Count > 0)
            {
                IComposer payload = _outgoingQueue.Dequeue();

                await _sessionObserver.SendComposerAsync(payload);
            }
        }

        _isProcessingQueue = false;
    }

    private void LogAndForget(Task task) =>
        task.LogAndForget(
            _logger,
            "Unhandled error while processing outgoing composer queue for player {PlayerId}",
            this.GetPrimaryKeyLong()
        );
}
