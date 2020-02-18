using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
struct LocalDbInstanceInfo
{
    public uint Size;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LocalDbApi.MaxName)]
    public string InstanceName;

    public bool Exists;
    public bool ConfigurationCorrupted;
    public bool IsRunning;
    public uint Major;
    public uint Minor;
    public uint Build;
    public uint Revision;
    public System.Runtime.InteropServices.ComTypes.FILETIME LastStartUtc;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LocalDbApi.MaxPath)]
    public string Connection;

    public bool IsShared;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LocalDbApi.MaxName)]
    public string SharedInstanceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LocalDbApi.MaxSid)]
    public string OwnerSID;

    public bool IsAutomatic;
}