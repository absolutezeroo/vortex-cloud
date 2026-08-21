using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Specs.Analysis.Client;
using Vortex.Specs.Analysis.Emulator;
using Vortex.Specs.Analysis.Reference;
using Vortex.Specs.Model;
using Vortex.Specs.Pipeline;
using Vortex.Specs.Sources;

namespace Vortex.Specs.Cli.Commands;

/// <summary>Read-only views of what each analyzer sees. Nothing here writes to disk.</summary>
public static class ScanCommand
{
    public static int Emulator(SpecWorkspace workspace)
    {
        CSharpSourceIndex index = CSharpSourceIndex.Build(
            workspace.RepositoryRoot,
            EmulatorProjects.Default
        );
        EmulatorScan scan = new EmulatorAnalyzer(workspace, index).Scan();

        Program.Heading($"Emulator — revision {scan.Revision}");
        Program.Rows([
            (
                "files indexed",
                scan.Incoming.Count > 0
                    ? index.Files.Count.ToString(CultureInfo.InvariantCulture)
                    : "0"
            ),
            ("incoming mapped", scan.Incoming.Count.ToString(CultureInfo.InvariantCulture)),
            ("outgoing mapped", scan.Outgoing.Count.ToString(CultureInfo.InvariantCulture)),
            ("handlers traced", scan.Flows.Count.ToString(CultureInfo.InvariantCulture)),
            (
                "incoming with a handler",
                scan.Incoming.Count(i => i.HandlerType is not null)
                    .ToString(CultureInfo.InvariantCulture)
            ),
            (
                "layouts read in full",
                scan.Incoming.Count(i => !i.LayoutIsPartial).ToString(CultureInfo.InvariantCulture)
            ),
        ]);

        Program.Heading("Header constants nothing maps");
        Console.WriteLine(
            $"  {scan.UnmappedHeaderConstants.Count} constants are declared but bound to no parser or serializer."
        );

        foreach (string constant in scan.UnmappedHeaderConstants.Take(15))
        {
            Console.WriteLine($"    {constant}");
        }

        Program.Heading("Parsers and serializers in no revision map");
        Console.WriteLine(
            $"  {scan.UnmappedImplementations.Count} classes exist but are unreachable."
        );

        foreach (string implementation in scan.UnmappedImplementations.Take(15))
        {
            Console.WriteLine($"    {implementation}");
        }

        return 0;
    }

    public static int Clients(SpecWorkspace workspace)
    {
        if (workspace.Clients.Count == 0)
        {
            Console.WriteLine("No client source trees found next to this checkout.");
            return 0;
        }

        // Which build the emulator speaks decides which client's header ids may be joined to it, so
        // the emulator scan runs first even though this command is about the clients.
        CSharpSourceIndex index = CSharpSourceIndex.Build(
            workspace.RepositoryRoot,
            ["Vortex.Revisions"]
        );
        string targetRevision = new EmulatorAnalyzer(workspace, index).Scan().Revision;

        foreach (SourceTree tree in workspace.Clients)
        {
            IClientAnalyzer analyzer =
                tree.Kind == SourceTreeKind.OfficialClient
                    ? new As3ClientAnalyzer(workspace, tree, targetRevision)
                    : new NitroClientAnalyzer(workspace, tree);

            ClientScan scan = analyzer.Scan();

            Program.Heading($"Client {scan.Origin}");
            Program.Rows([
                ("authority", scan.Authority.Wire()),
                ("targets this revision", scan.TargetsSameRevision ? "yes" : "no"),
                ("packets read", scan.Packets.Count.ToString(CultureInfo.InvariantCulture)),
                (
                    "client to server",
                    scan.Packets.Count(p => p.Direction == PacketDirection.Incoming)
                        .ToString(CultureInfo.InvariantCulture)
                ),
                (
                    "server to client",
                    scan.Packets.Count(p => p.Direction == PacketDirection.Outgoing)
                        .ToString(CultureInfo.InvariantCulture)
                ),
                (
                    "fields named",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} of {1}",
                        scan.Packets.SelectMany(p => p.Fields).Count(f => f.Name is not null),
                        scan.Packets.Sum(p => p.Fields.Count)
                    )
                ),
                (
                    "bound to a header id",
                    scan.Packets.Count(p => p.HeaderId is not null)
                        .ToString(CultureInfo.InvariantCulture)
                ),
                ("unresolved", scan.Unresolved.Count.ToString(CultureInfo.InvariantCulture)),
            ]);

            if (!scan.TargetsSameRevision)
            {
                Console.WriteLine(
                    "  Header ids from this tree are for another client build and are never compared."
                );
            }
        }

        return 0;
    }

    public static int References(SpecWorkspace workspace)
    {
        if (workspace.References.Count == 0)
        {
            Console.WriteLine("No reference emulator trees found next to this checkout.");
            return 0;
        }

        foreach (SourceTree tree in workspace.References)
        {
            ReferenceScan scan = new ArcturusReferenceAnalyzer(workspace, tree).Scan();

            Program.Heading($"Reference {scan.Origin}");
            Program.Rows([
                ("authority", scan.Authority.Wire()),
                ("handlers read", scan.Behaviours.Count.ToString(CultureInfo.InvariantCulture)),
                (
                    "handlers that answer",
                    scan.Behaviours.Count(b => b.Outgoing.Count > 0)
                        .ToString(CultureInfo.InvariantCulture)
                ),
                (
                    "handlers with guards",
                    scan.Behaviours.Count(b => b.Checks.Count > 0)
                        .ToString(CultureInfo.InvariantCulture)
                ),
                ("composers read", scan.Composers.Count.ToString(CultureInfo.InvariantCulture)),
                ("unresolved", scan.Unresolved.Count.ToString(CultureInfo.InvariantCulture)),
            ]);

            Console.WriteLine(
                "  What this implementation does is evidence about this implementation. It is not"
            );
            Console.WriteLine("  evidence about Habbo, however long it has been in production.");
        }

        return 0;
    }
}
