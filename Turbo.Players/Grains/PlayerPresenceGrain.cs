using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
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

    private readonly PlayerInventoryModule _inventoryModule;

    private readonly Queue<IComposer> _outgoingQueue = new();
    internal readonly PlayerPresenceLiveState _state;
    private readonly PlayerWalletModule _walletModule;
    private bool _isProcessingQueue;
    private StreamSubscriptionHandle<RoomOutbound>? _roomOutboundSub;

    private ISessionContextObserver? _sessionObserver;

    public PlayerPresenceGrain(IGrainFactory grainFactory, IEventPublisher events)
    {
        _grainFactory = grainFactory;
        _events = events;

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
            _outgoingQueue.Enqueue(composer);

            _ = ProcessOutgoingQueueAsync();
        }

        return Task.CompletedTask;
    }

    public Task SendComposerAsync(params IComposer[] composers)
    {
        if (composers.Length > 0)
        {
            foreach (IComposer composer in composers)
            {
                _outgoingQueue.Enqueue(composer);
            }

            _ = ProcessOutgoingQueueAsync();
        }

        return Task.CompletedTask;
    }

    public override Task OnActivateAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _outgoingQueue.Clear();

        return Task.CompletedTask;
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
}
