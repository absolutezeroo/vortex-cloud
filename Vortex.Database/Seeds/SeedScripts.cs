using System;
using System.IO;
using System.Reflection;

namespace Vortex.Database.Seeds;

/// <summary>
/// Loads the reference-data seed scripts embedded from <c>Vortex.Database/Seeds/*.sql</c>, so a
/// migration can apply one with <c>migrationBuilder.Sql(SeedScripts.Read("x.sql"))</c>. The scripts
/// use <c>INSERT IGNORE</c> and are therefore safe to run against a database that already holds
/// some of the rows.
/// </summary>
public static class SeedScripts
{
    private const string ResourcePrefix = "Vortex.Database.Seeds.";

    public static string Read(string fileName)
    {
        Assembly assembly = typeof(SeedScripts).Assembly;
        string resourceName = ResourcePrefix + fileName;

        using Stream stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Seed script '{fileName}' is not embedded in {assembly.GetName().Name}. "
                    + "Check the EmbeddedResource glob in Vortex.Database.csproj."
            );

        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }
}
