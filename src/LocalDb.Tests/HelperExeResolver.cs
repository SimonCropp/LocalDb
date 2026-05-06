// Shared helper-exe lookup used by the multi-process race tests.
// AppContext.BaseDirectory points at <root>/src/LocalDb.Tests/bin/<Config>/net10.0/.
// The MultiProcessHelper builds to a sibling project's bin folder; reuse the same
// configuration name so Debug and Release runs both find their matching helper.

static class HelperExeResolver
{
    public static string Resolve()
    {
        var basedir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd('/', '\\'));
        var configFolder = basedir.Parent ?? throw new InvalidOperationException($"Unexpected base directory layout: {basedir}");
        var srcFolder = configFolder.Parent?.Parent?.Parent ?? throw new InvalidOperationException($"Unexpected base directory layout: {basedir}");
        var helperPath = Path.Combine(
            srcFolder.FullName,
            "LocalDb.MultiProcessHelper",
            "bin",
            configFolder.Name,
            "net10.0",
            "LocalDb.MultiProcessHelper.exe");

        if (!File.Exists(helperPath))
        {
            throw new FileNotFoundException(
                $"Helper exe not found at {helperPath}. Build LocalDb.MultiProcessHelper first (the test csproj references it as a build-only ProjectReference, so a clean dotnet build of the test project should produce it).");
        }
        return helperPath;
    }
}
