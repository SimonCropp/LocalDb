using System.Runtime.InteropServices;

static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int CopyFile2(
        string pwszExistingFileName,
        string pwszNewFileName,
        ref COPYFILE2_EXTENDED_PARAMETERS pExtendedParameters);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int CopyFile2(
        string pwszExistingFileName,
        string pwszNewFileName,
        IntPtr pExtendedParameters);

    [StructLayout(LayoutKind.Sequential)]
    internal struct COPYFILE2_EXTENDED_PARAMETERS
    {
        public uint dwSize;
        public uint dwCopyFlags;
        public IntPtr pfCancel;
        public IntPtr pProgressRoutine;
        public IntPtr pvCallbackContext;
    }

    internal const uint COPY_FILE_FAIL_IF_EXISTS = 0x00000001;
    internal const uint COPY_FILE_NO_BUFFERING = 0x00001000;

    internal const int S_OK = 0;

    internal static Task CopyFileAsync(string source, string destination, bool failIfExists = false, bool noBuffering = true) =>
        Task.Run(() =>
        {
            uint copyFlags = 0;

            if (failIfExists)
            {
                copyFlags |= COPY_FILE_FAIL_IF_EXISTS;
            }

            if (noBuffering)
            {
                copyFlags |= COPY_FILE_NO_BUFFERING;
            }

            var parameters = new COPYFILE2_EXTENDED_PARAMETERS
            {
                dwSize = (uint)Marshal.SizeOf<COPYFILE2_EXTENDED_PARAMETERS>(),
                dwCopyFlags = copyFlags,
                pfCancel = IntPtr.Zero,
                pProgressRoutine = IntPtr.Zero,
                pvCallbackContext = IntPtr.Zero
            };

            var result = CopyFile2(source, destination, ref parameters);

            if (result != S_OK)
            {
                throw new IOException($"Failed to copy file from '{source}' to '{destination}'. HRESULT: 0x{result:X8}", Marshal.GetExceptionForHR(result));
            }
        });
}
