using System.IO;

static class FileExtensions
{
    public static void FlushDirectory(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            File.Delete(file);
        }
    }
}