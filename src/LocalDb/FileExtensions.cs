static class FileExtensions
{
    public static async Task WriteFileAsync(string destFile, byte[] bytes)
    {
#if NET6_0_OR_GREATER
        using var handle = File.OpenHandle(
            destFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            FileOptions.Asynchronous);
        await RandomAccess.WriteAsync(handle, bytes, 0);
#elif NET5_0_OR_GREATER
        await using var destStream = new FileStream(
            destFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await destStream.WriteAsync(bytes);
#else
        using var destStream = new FileStream(
            destFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await destStream.WriteAsync(bytes, 0, bytes.Length);
#endif
    }

    public static void MarkFileAsWritable(string file)
    {
        var info = new FileInfo(file);
        info.Attributes &= ~FileAttributes.ReadOnly;
    }

    public static void FlushDirectory(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            File.Delete(file);
        }
    }

    // For running under elevated VS
    // LocalDb will no be able to access the created files
    public static void ResetAccess(this DirectoryInfo directoryInfo)
    {
        var accessControl = directoryInfo.GetAccessControl();
        var everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        var accessRule = new FileSystemAccessRule(everyone, FileSystemRights.FullControl | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
        accessControl.AddAccessRule(accessRule);
        directoryInfo.SetAccessControl(accessControl);
        directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
    }
}