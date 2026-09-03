using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Database.Context;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Sound;
using Vortex.Primitives.Sound.Snapshots;
using Vortex.Protocol.Messages.Outgoing.Sound;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The room's jukebox: which disks are loaded, and which one is playing right now.
/// </summary>
/// <remarks>
/// <para>
/// A loaded disk is one <c>furniture</c> row with <c>jukebox_id</c> set — the same shape the wired
/// chest uses. It never becomes a second row anywhere, so there is no window in which the disk is
/// both in the jukebox and in its owner's hands, and no compensating delete to get wrong. The two
/// moves live in <see cref="JukeboxDiskStore" />, one guarded <c>UPDATE</c> each.
/// </para>
/// <para>
/// Playback is a clock, not a queue: the playlist is a loop, and where it is depends only on when
/// the current song started. Everyone in the room is handed the same offset, which is what makes two
/// clients hear the same bar. The tick exists solely to push the next <c>NowPlaying</c> at a song
/// boundary — the client does not advance a jukebox playlist on its own, its own "song finished"
/// handler for this controller is empty.
/// </para>
/// </remarks>
public sealed class RoomJukeboxSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    /// <summary>The loaded disks in play order, or null when they have not been read yet.</summary>
    private ImmutableArray<JukeboxDiskRow>? _loaded;

    /// <summary>The jukebox the cached rows belong to, so a swapped jukebox is not read stale.</summary>
    private int _loadedJukeboxId;

    private int _playingIndex;

    /// <summary>Room clock reading at which the current song started, or 0 when nothing is playing.</summary>
    private long _songStartedAtMs;

    /// <summary>The jukebox standing in this room, if there is one.</summary>
    /// <remarks>
    /// Resolved by walking the room's items because the client's requests carry no identifier: it
    /// asks for "the playlist", and the room has to know which furniture that means. A room holding
    /// two jukeboxes uses the first, which is arbitrary but stable — and matches what the client's
    /// own single controller can represent.
    /// </remarks>
    private IRoomItem? FindJukebox() =>
        _roomGrain._state.ItemsById.Values.FirstOrDefault(item =>
            item.Definition.LogicName == SoundLogicNames.Jukebox
        );

    public async Task<JukeboxPlaylistSnapshot> GetPlaylistAsync(CancellationToken ct)
    {
        IRoomItem? jukebox = FindJukebox();

        if (jukebox is null)
        {
            return JukeboxPlaylistSnapshot.Empty;
        }

        return ToPlaylist(await ReadAsync(jukebox.ObjectId.Value, ct).ConfigureAwait(true));
    }

    public async Task<JukeboxLoadResult> AddDiskAsync(
        ActionContext ctx,
        int diskItemId,
        CancellationToken ct
    )
    {
        IRoomItem? jukebox = FindJukebox();

        // Loading a jukebox is a decoration right, the same one that governs placing furniture.
        // Checked here rather than in the handler because the client can send this message without
        // ever having opened the editor.
        if (
            jukebox is null
            || ctx.PlayerId <= 0
            || !await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx).ConfigureAwait(true)
        )
        {
            return JukeboxLoadResult.Refused;
        }

        int jukeboxId = jukebox.ObjectId.Value;
        ImmutableArray<JukeboxDiskRow> loaded = await ReadAsync(jukeboxId, ct).ConfigureAwait(true);

        if (loaded.Length >= _roomGrain._roomConfig.JukeboxCapacity)
        {
            return new JukeboxLoadResult
            {
                Outcome = JukeboxLoadOutcome.Full,
                Playlist = ToPlaylist(loaded),
            };
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        if (
            await JukeboxDiskStore
                .LoadAsync(dbCtx, diskItemId, (int)ctx.PlayerId, jukeboxId, ct)
                .ConfigureAwait(true) == 0
        )
        {
            return JukeboxLoadResult.Refused;
        }

        _loaded = null;

        // The row is the jukebox's now, and nothing else tells the owner's inventory: it is a cache
        // built at activation, so without this the disk stays on their screen and placing it looks
        // like it should work.
        await _roomGrain
            ._grainFactory.GetInventoryGrain(ctx.PlayerId)
            .ReloadFurnitureAsync(ct)
            .ConfigureAwait(true);

        return await PublishAsync(jukeboxId, ct).ConfigureAwait(true);
    }

    public async Task<JukeboxLoadResult> RemoveDiskAsync(
        ActionContext ctx,
        int index,
        CancellationToken ct
    )
    {
        IRoomItem? jukebox = FindJukebox();

        if (
            jukebox is null
            || ctx.PlayerId <= 0
            || !await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx).ConfigureAwait(true)
        )
        {
            return JukeboxLoadResult.Refused;
        }

        int jukeboxId = jukebox.ObjectId.Value;
        ImmutableArray<JukeboxDiskRow> loaded = await ReadAsync(jukeboxId, ct).ConfigureAwait(true);

        // The client names the disk by where it sat in the list it was last sent. That list can have
        // moved on, and guessing at what a stale index meant would hand back the wrong disk.
        if (index < 0 || index >= loaded.Length)
        {
            return JukeboxLoadResult.Refused;
        }

        JukeboxDiskRow disk = loaded[index];

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        if (
            await JukeboxDiskStore
                .UnloadAsync(dbCtx, disk.DiskId, jukeboxId, ct)
                .ConfigureAwait(true) == 0
        )
        {
            return JukeboxLoadResult.Refused;
        }

        _loaded = null;

        // Back into the owner's hands, and the owner is whoever it was all along — not whoever is
        // emptying the jukebox.
        await _roomGrain
            ._grainFactory.GetInventoryGrain(disk.OwnerId)
            .ReloadFurnitureAsync(ct)
            .ConfigureAwait(true);

        return await PublishAsync(jukeboxId, ct).ConfigureAwait(true);
    }

    public async Task<NowPlayingSnapshot> GetNowPlayingAsync(CancellationToken ct)
    {
        IRoomItem? jukebox = FindJukebox();

        if (jukebox is null)
        {
            return NowPlayingSnapshot.Silent;
        }

        return BuildNowPlaying(
            await ReadAsync(jukebox.ObjectId.Value, ct).ConfigureAwait(true),
            _roomGrain.NowMs()
        );
    }

    /// <summary>
    /// One tick: pushes the next <c>NowPlaying</c> when the playing song has run out.
    /// </summary>
    /// <remarks>
    /// Costs one null check in a room with no jukebox, which is nearly every room: the playlist is
    /// read on request, never by the tick, so a room nobody has asked about stays untouched.
    /// </remarks>
    public async Task ProcessAsync(long now, CancellationToken ct)
    {
        if (_loaded is not { } loaded || loaded.IsEmpty || _songStartedAtMs == 0)
        {
            return;
        }

        if (now - _songStartedAtMs < LengthMsOf(loaded, _playingIndex))
        {
            return;
        }

        _playingIndex = (_playingIndex + 1) % loaded.Length;
        _songStartedAtMs = now;

        await _roomGrain
            .SendComposerToRoomAsync(ToComposer(BuildNowPlaying(loaded, now)))
            .ConfigureAwait(true);
    }

    /// <summary>Tells the whole room what is loaded and where playback now stands.</summary>
    /// <remarks>
    /// A jukebox is shared furniture: a playlist only the person who touched it could see would be
    /// wrong on every other screen in the room.
    /// </remarks>
    private async Task<JukeboxLoadResult> PublishAsync(int jukeboxId, CancellationToken ct)
    {
        ImmutableArray<JukeboxDiskRow> loaded = await ReadAsync(jukeboxId, ct).ConfigureAwait(true);
        JukeboxPlaylistSnapshot playlist = ToPlaylist(loaded);

        await _roomGrain
            .SendComposerToRoomAsync(
                new JukeboxSongDisksMessageComposer
                {
                    Disks = playlist.Disks,
                    Capacity = playlist.Capacity,
                }
            )
            .ConfigureAwait(true);

        await _roomGrain
            .SendComposerToRoomAsync(ToComposer(BuildNowPlaying(loaded, _roomGrain.NowMs())))
            .ConfigureAwait(true);

        return new JukeboxLoadResult { Outcome = JukeboxLoadOutcome.Moved, Playlist = playlist };
    }

    private JukeboxPlaylistSnapshot ToPlaylist(ImmutableArray<JukeboxDiskRow> loaded) =>
        new()
        {
            Disks =
            [
                .. loaded.Select(disk => new SongDiskSnapshot
                {
                    DiskId = disk.DiskId,
                    SongId = SongIdOf(disk),
                }),
            ],
            Capacity = _roomGrain._roomConfig.JukeboxCapacity,
        };

    private static NowPlayingMessageComposer ToComposer(NowPlayingSnapshot now) =>
        new()
        {
            CurrentSongId = now.CurrentSongId,
            CurrentIndex = now.CurrentIndex,
            NextSongId = now.NextSongId,
            NextIndex = now.NextIndex,
            SyncCountMs = now.SyncCountMs,
        };

    private NowPlayingSnapshot BuildNowPlaying(ImmutableArray<JukeboxDiskRow> loaded, long now)
    {
        if (loaded.IsEmpty)
        {
            _songStartedAtMs = 0;

            return NowPlayingSnapshot.Silent;
        }

        // A playlist that shrank under the playing index restarts rather than reading past its end.
        if (_playingIndex >= loaded.Length || _songStartedAtMs == 0)
        {
            _playingIndex = 0;
            _songStartedAtMs = now;
        }

        int nextIndex = (_playingIndex + 1) % loaded.Length;

        return new NowPlayingSnapshot
        {
            CurrentSongId = SongIdOf(loaded[_playingIndex]),
            CurrentIndex = _playingIndex,
            NextSongId = SongIdOf(loaded[nextIndex]),
            NextIndex = nextIndex,
            SyncCountMs = (int)(now - _songStartedAtMs),
        };
    }

    /// <summary>
    /// How long the song on the disk at <paramref name="index" /> runs.
    /// </summary>
    /// <remarks>
    /// A disk whose song this hotel does not ship would otherwise have length 0 and be stepped over
    /// twenty times a second. It is given a minute instead: the playlist keeps moving, and the empty
    /// slot is audible as silence rather than as a spinning room.
    /// </remarks>
    private int LengthMsOf(ImmutableArray<JukeboxDiskRow> loaded, int index) =>
        _roomGrain._songProvider.TryGetSong(SongIdOf(loaded[index]))?.LengthMs is > 0 and int length
            ? length
            : 60_000;

    /// <summary>
    /// The song pressed on a disk. The row carries the extra-data blob rather than the bare string
    /// the client reads, so the legacy view is rebuilt the same way the inventory builds it — one
    /// parser for one format.
    /// </summary>
    private int SongIdOf(JukeboxDiskRow disk) =>
        SongDiskExtraData.ReadSongId(
            _roomGrain
                ._stuffDataFactory.CreateStuffDataFromJson(StuffDataType.LegacyKey, disk.ExtraData)
                .GetLegacyString()
        );

    private async Task<ImmutableArray<JukeboxDiskRow>> ReadAsync(
        int jukeboxId,
        CancellationToken ct
    )
    {
        if (_loaded is { } cached && _loadedJukeboxId == jukeboxId)
        {
            return cached;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        List<JukeboxDiskRow> rows = await JukeboxDiskStore
            .ReadAsync(dbCtx, jukeboxId, _roomGrain._roomConfig.JukeboxCapacity, ct)
            .ConfigureAwait(true);

        _loaded = [.. rows];
        _loadedJukeboxId = jukeboxId;

        return _loaded.Value;
    }
}
