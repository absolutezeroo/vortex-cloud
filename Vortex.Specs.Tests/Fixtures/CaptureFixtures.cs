using System;
using System.IO;

namespace Vortex.Specs.Tests.Fixtures;

/// <summary>
/// Synthetic captures written to a temp directory for the importer tests.
/// </summary>
/// <remarks>
/// Deliberately not checked into <c>docs/habbo-specs/evidence/captures</c>. A fabricated capture
/// sitting in the evidence tree would be indistinguishable from a real recording and would promote
/// invented behaviour to <c>capture_confirmed</c> — the single worst thing this system could do.
/// Test fixtures live here, under a name that says what they are.
/// </remarks>
public sealed class TemporaryCapture : IDisposable
{
    private readonly string _directory;

    public TemporaryCapture(string fileName, string json)
    {
        _directory = Path.Combine(
            Path.GetTempPath(),
            "vortex-specs-tests",
            Guid.NewGuid().ToString("n")
        );
        Directory.CreateDirectory(_directory);
        Path_ = Path.Combine(_directory, fileName);
        File.WriteAllText(Path_, json);
    }

    public string Path_ { get; }

    public string Directory_ => _directory;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A locked temp file is not worth failing a test run over; the OS reclaims it.
        }
    }

    public const string MoveFurnitureOfficial = """
        {
          "id": "synthetic-move-furniture",
          "source": "official",
          "revision": "WIN63-202607011411-782849652",
          "note": "SYNTHETIC test fixture, not a real recording",
          "messages": [
            { "index": 0, "direction": "client_to_server", "name": "MoveObject",
              "fields": { "object_id": "4021", "x": "7", "y": "3", "rotation": "2" } },
            { "index": 1, "direction": "server_to_client", "name": "ObjectUpdate", "recipient": "room_users" },
            { "index": 2, "direction": "client_to_server", "name": "MoveObject",
              "fields": { "object_id": "4021", "x": "99", "y": "99", "rotation": "2" } },
            { "index": 3, "direction": "server_to_client", "name": "NotificationDialog", "recipient": "actor" },
            { "index": 4, "direction": "server_to_client", "name": "ObjectUpdate", "recipient": "actor" }
          ]
        }
        """;

    /// <summary>The same triggers, answered differently: what a differential run looks like.</summary>
    public const string MoveFurnitureEmulator = """
        {
          "id": "synthetic-move-furniture-vortex",
          "source": "vortex",
          "messages": [
            { "index": 0, "direction": "client_to_server", "name": "MoveObject" },
            { "index": 1, "direction": "server_to_client", "name": "ObjectUpdate", "recipient": "actor" },
            { "index": 2, "direction": "client_to_server", "name": "MoveObject" },
            { "index": 3, "direction": "server_to_client", "name": "ObjectUpdate", "recipient": "actor" }
          ]
        }
        """;
}
