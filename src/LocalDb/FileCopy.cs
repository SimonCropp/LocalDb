static class FileCopy
{
    extension(File)
    {
        public static Task CopyAsync(string source, string destination) =>
            Task.Run(() => File.Copy(source, destination));
    }
}
