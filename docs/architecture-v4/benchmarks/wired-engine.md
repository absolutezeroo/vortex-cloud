# Wired engine baseline

Mean microseconds per `ProcessWiredAsync`, over 2,000 iterations after 200 warm-up ticks.

Only comparable against numbers from the same machine. Regenerate with `VORTEX_BENCH=1 dotnet test Vortex.Rooms.Tests --filter WiredEngineBenchmark`.

| Scenario | µs / tick |
|---|---:|
| empty room | 0.22 |
| loaded room, no wired (1k items) | 0.22 |
| loaded room, idle trigger (1k items) | 0.70 |
| chain firing every tick | 0.33 |
| event storm (2k events per tick) | 1277.97 |

A room ticks every `RoomTickMs` (50 ms = 50,000 µs), so the last column against that number is the share of one room's budget the wired step spends.

Measured against `FakeWiredRoomHost`, so these are the engine's own costs: the orchestration, the index, the scheduler, the pile resolution. What a real room spends looking items up in its own state is not in here.

## Measured on

- Microsoft Windows NT 10.0.26200.0
- 12 logical processors
- .NET 10.0.9
