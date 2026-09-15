// Shared lookup for helper executables that tests launch as separate processes.
// AppContext.BaseDirectory points at <root>/src/<TestProject>/bin/<Config>/net10.0/.
// Each helper builds to its own sibling project's bin folder; reuse the same
// configuration name so Debug and Release runs both find their matching helper.

static class HelperExeResolver
{
    public static string Resolve(string project = "LocalDb.MultiProcessHelper")
    {
        var basedir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd('/', '\\'));
        var configFolder = basedir.Parent ?? throw new InvalidOperationException($"Unexpected base directory layout: {basedir}");
        var srcFolder = configFolder.Parent?.Parent?.Parent ?? throw new InvalidOperationException($"Unexpected base directory layout: {basedir}");
        var helperPath = Path.Combine(
            srcFolder.FullName,
            project,
            "bin",
            configFolder.Name,
            "net10.0",
            $"{project}.exe");

        if (!File.Exists(helperPath))
        {
            throw new FileNotFoundException(
                $"Helper exe not found at {helperPath}. Build {project} first (the test csproj references it as a build-only ProjectReference, so a clean dotnet build of the test project should produce it).");
        }
        return helperPath;
    }
}
