static class FileCopy
{
    extension(File)
    {
        public static Task CopyAsync(string source, string destination, Cancel cancel = default) =>
            Task.Run(() =>
            {
                cancel.ThrowIfCancellationRequested();
                File.Copy(source, destination);
            }, cancel);
    }
}
