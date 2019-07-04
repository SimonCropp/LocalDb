using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
struct LocalDbInstanceInfo
{
    public uint Size;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxName)]
    public string InstanceName;

    public bool Exists;
    public bool ConfigurationCorrupted;
    public bool IsRunning;
    public uint Major;
    public uint Minor;
    public uint Build;
    public uint Revision;
    public FILETIME LastStartUtc;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxPath)]
    public string Connection;

    public bool IsShared;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxName)]
    public string SharedInstanceName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = UnmanagedLocalDbApi.MaxSid)]
    public string OwnerSID;

    public bool IsAutomatic;
}