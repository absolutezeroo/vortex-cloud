using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Captures;
using Vortex.Specs.Model;
using Vortex.Specs.Persistence;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

public static class CaptureCommand
{
    public static int Import(SpecWorkspace workspace, CommandLine command)
    {
        if (command.Positional.Count == 0)
        {
            Console.Error.WriteLine("import-capture needs a path to a capture file.");
            return 2;
        }

        string path = command.Positional[0];

        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"No such file: {path}");
            return 2;
        }

        CaptureImporter importer = new();
        CaptureDocument capture;

        try
        {
            capture = importer.Read(path);
        }
        catch (CaptureImportException error)
        {
            Console.Error.WriteLine(error.Message);
            return 2;
        }

        // The registry is what names a message that was captured as a bare header id, so the scan is
        // worth its cost here even though the command only reads one file.
        CSharpSourceIndex index = CSharpSourceIndex.Build(
            workspace.RepositoryRoot,
            ["Vortex.Revisions"]
        );
        RevisionRegistry registry = new EmulatorAnalyzer(workspace, index).Scan().Registry;

        IReadOnlyList<CaptureObservation> observations = importer.Observe(capture, registry);

        Program.Heading($"Capture {capture.Id}");
        Program.Rows([
            ("source", capture.Source.ToString().ToLowerInvariant()),
            ("authority", capture.Authority.Wire()),
            ("revision", capture.Revision ?? "unstated"),
            ("messages", capture.Messages.Count.ToString(CultureInfo.InvariantCulture)),
            ("observations", observations.Count.ToString(CultureInfo.InvariantCulture)),
        ]);

        if (capture.Source == CaptureSource.Unknown)
        {
            Console.WriteLine();
            Console.WriteLine(
                "  This capture does not say where it was recorded, so it carries no weight: set"
            );
            Console.WriteLine(
                "  \"source\": \"official\" only if it really came from a Habbo server."
            );
        }

        Program.Heading("Observations");

        foreach (CaptureObservation observation in observations)
        {
            Console.WriteLine($"  {observation.TriggerPacket}");

            foreach (string emitted in observation.EmittedPackets)
            {
                Console.WriteLine($"      -> {emitted}");
            }

            if (observation.EmittedPackets.Count == 0)
            {
                Console.WriteLine(
                    "      -> (the server sent nothing before the client spoke again)"
                );
            }
        }

        int unnamed = capture.Messages.Count(m => m.Name is null && m.Header is not null);

        if (unnamed > 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"  {unnamed} messages carried only a header id. Those the registry could not name were"
            );
            Console.WriteLine("  left out rather than guessed at.");
        }

        if (command.Has("write"))
        {
            SpecStore store = new(workspace.OutputRoot);
            SpecWriteResult result = store.Write(
                Path.Combine(
                    "evidence",
                    "captures",
                    SpecStore.FileName(capture.Id) + ".observations.yaml"
                ),
                "capture-observations",
                SpecWriter.CaptureObservations(capture, observations),
                command.Has("force")
            );

            Console.WriteLine();
            Console.WriteLine(
                $"  {result.Outcome.ToString().ToLowerInvariant()}: {workspace.Relative(result.Path)}"
            );

            if (result.Detail is not null)
            {
                Console.WriteLine($"  {result.Detail}");
            }
        }

        return 0;
    }
}
