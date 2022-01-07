using System.Security.AccessControl;
using System.Security.Principal;

static class FileExtensions
{
    public static void MarkFileAsWritable(string dataFile)
    {
        var di = new FileInfo(dataFile);
        di.Attributes &= ~FileAttributes.ReadOnly;
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