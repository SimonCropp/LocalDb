using System.IO;
using System.Threading;
using System.Threading.Tasks;

static class FileExtensions
{
    static FileOptions fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
    static int bufferSize = 4096;

    public static async Task Copy(string sourceFile, string destinationFile, CancellationToken cancellation = default)
    {
        using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))
        using (var destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, fileOptions))
        {
            await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellation);
        }
    }
}