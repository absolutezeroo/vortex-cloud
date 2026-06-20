using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Providers;
using Turbo.Primitives.Rooms.Snapshots.Mapping;

namespace Turbo.Rooms.Providers;

public sealed class RoomModelProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IRoomModelProvider> logger
) : IRoomModelProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IRoomModelProvider> _logger = logger;

    private ImmutableDictionary<int, RoomModelSnapshot> _modelsById = ImmutableDictionary<
        int,
        RoomModelSnapshot
    >.Empty;

    public RoomModelSnapshot GetModelById(int modelId)
    {
        return _modelsById.TryGetValue(modelId, out RoomModelSnapshot? model)
            ? model
            : throw new KeyNotFoundException($"Room model not found: ModelId={modelId}");
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<RoomModelEntity> entities = await dbCtx
                .RoomModels.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _modelsById = entities
                .Select(x =>
                {
                    string modelData = CleanModelString(x.Model);
                    CompiledRoomModelSnapshot compiledModel = CompileModelFromString(modelData);

                    return new RoomModelSnapshot
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Model = modelData,
                        DoorX = x.DoorX,
                        DoorY = x.DoorY,
                        DoorRotation = x.DoorRotation,
                        Width = compiledModel.Width,
                        Height = compiledModel.Height,
                        Size = compiledModel.Width * compiledModel.Height,
                        BaseHeights = compiledModel.Heights,
                        BaseFlags = compiledModel.Flags
                    };
                })
                .ToImmutableDictionary(x => x.Id);

            _logger.LogInformation(
                "Loaded room models: TotalModels={TotalModelCount}",
                _modelsById.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string CleanModelString(string model)
    {
        return model.Trim().ToLower().Replace("\r\n", "\r").Replace("\n", "\r");
    }

    private static CompiledRoomModelSnapshot CompileModelFromString(string model)
    {
        List<string> rows = SplitLines(model);

        if (rows.Count == 0)
        {
            throw new InvalidDataException("Room model data is empty.");
        }

        int height = rows.Count;
        int width = rows.Max(x => x.Length);
        int size = width * height;
        Altitude[] heights = new Altitude[size];
        RoomTileFlags[] flags = new RoomTileFlags[size];

        for (int y = 0; y < height; y++)
        {
            string row = rows[y];

            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                char ch = x < row.Length ? row[x] : 'x';

                if (ch.Equals('x'))
                {
                    heights[idx] = Altitude.Zero;
                    flags[idx] = RoomTileFlags.Disabled | RoomTileFlags.StackBlocked;
                }
                else
                {
                    int heightIndex = "abcdefghijklmnopqrstuvwxyz".IndexOf(ch);
                    Altitude tileHeight =
                        heightIndex == -1
                            ? Altitude.FromInt(int.TryParse(ch.ToString(), out int h) ? h : 0)
                            : Altitude.FromInt(heightIndex + 10);

                    heights[idx] = tileHeight;
                    flags[idx] = RoomTileFlags.Open;
                }
            }
        }

        return new CompiledRoomModelSnapshot
        {
            Width = width,
            Height = height,
            Heights = heights,
            Flags = flags
        };
    }

    private static List<string> SplitLines(string s)
    {
        List<string> lines = new();

        using StringReader sr = new(s);

        string? line;

        while ((line = sr.ReadLine()) is not null)
        {
            lines.Add(line);
        }

        return lines;
    }
}
