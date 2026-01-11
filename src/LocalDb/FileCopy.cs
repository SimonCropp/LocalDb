// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Style", "IDE1006:Naming Styles")]
static class FileCopy
{
    extension(File)
    {
        public static Task CopyAsync(string source, string destination) =>
            CopyFileAsync(source, destination);
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern int CopyFile2(
        string pwszExistingFileName,
        string pwszNewFileName,
        ref COPYFILE2_EXTENDED_PARAMETERS pExtendedParameters);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate COPYFILE2_MESSAGE_ACTION CopyProgressRoutine(
        ref COPYFILE2_MESSAGE pMessage,
        IntPtr pvCallbackContext);

    [StructLayout(LayoutKind.Sequential)]
    struct COPYFILE2_EXTENDED_PARAMETERS
    {
        public uint dwSize;
        public uint dwCopyFlags;
        public IntPtr pfCancel;
        public IntPtr pProgressRoutine;
        public IntPtr pvCallbackContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct COPYFILE2_MESSAGE
    {
        public COPYFILE2_MESSAGE_TYPE Type;
        public uint dwPadding;
        public COPYFILE2_MESSAGE_INFO Info;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct COPYFILE2_MESSAGE_INFO
    {
        [FieldOffset(0)]
        public ChunkStarted ChunkStarted;
        [FieldOffset(0)]
        public ChunkFinished ChunkFinished;
        [FieldOffset(0)]
        public StreamStarted StreamStarted;
        [FieldOffset(0)]
        public StreamFinished StreamFinished;
        [FieldOffset(0)]
        public PollContinue PollContinue;
        [FieldOffset(0)]
        public Error Error;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ChunkStarted
    {
        public uint dwStreamNumber;
        public uint dwReserved;
        public IntPtr hSourceFile;
        public IntPtr hDestinationFile;
        public ulong uliChunkNumber;
        public ulong uliChunkSize;
        public ulong uliStreamSize;
        public ulong uliTotalFileSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ChunkFinished
    {
        public uint dwStreamNumber;
        public uint dwFlags;
        public IntPtr hSourceFile;
        public IntPtr hDestinationFile;
        public ulong uliChunkNumber;
        public ulong uliChunkSize;
        public ulong uliStreamSize;
        public ulong uliStreamBytesTransferred;
        public ulong uliTotalFileSize;
        public ulong uliTotalBytesTransferred;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct StreamStarted
    {
        public uint dwStreamNumber;
        public uint dwReserved;
        public IntPtr hSourceFile;
        public IntPtr hDestinationFile;
        public ulong uliStreamSize;
        public ulong uliTotalFileSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct StreamFinished
    {
        public uint dwStreamNumber;
        public uint dwReserved;
        public IntPtr hSourceFile;
        public IntPtr hDestinationFile;
        public ulong uliStreamSize;
        public ulong uliStreamBytesTransferred;
        public ulong uliTotalFileSize;
        public ulong uliTotalBytesTransferred;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PollContinue
    {
        public uint dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Error
    {
        public COPYFILE2_COPY_PHASE CopyPhase;
        public uint dwStreamNumber;
        public int hrFailure;
        public uint dwReserved;
        public ulong uliChunkNumber;
        public ulong uliStreamSize;
        public ulong uliStreamBytesTransferred;
        public ulong uliTotalFileSize;
        public ulong uliTotalBytesTransferred;
    }

    enum COPYFILE2_MESSAGE_TYPE
    {
        COPYFILE2_CALLBACK_NONE = 0,
        COPYFILE2_CALLBACK_CHUNK_STARTED = 1,
        COPYFILE2_CALLBACK_CHUNK_FINISHED = 2,
        COPYFILE2_CALLBACK_STREAM_STARTED = 3,
        COPYFILE2_CALLBACK_STREAM_FINISHED = 4,
        COPYFILE2_CALLBACK_POLL_CONTINUE = 5,
        COPYFILE2_CALLBACK_ERROR = 6,
    }

    enum COPYFILE2_MESSAGE_ACTION
    {
        COPYFILE2_PROGRESS_CONTINUE = 0,
        COPYFILE2_PROGRESS_CANCEL = 1,
        COPYFILE2_PROGRESS_STOP = 2,
        COPYFILE2_PROGRESS_QUIET = 3,
        COPYFILE2_PROGRESS_PAUSE = 4,
    }

    enum COPYFILE2_COPY_PHASE
    {
        COPYFILE2_PHASE_NONE = 0,
        COPYFILE2_PHASE_PREPARE_SOURCE = 1,
        COPYFILE2_PHASE_PREPARE_DEST = 2,
        COPYFILE2_PHASE_READ_SOURCE = 3,
        COPYFILE2_PHASE_WRITE_DESTINATION = 4,
        COPYFILE2_PHASE_SERVER_COPY = 5,
        COPYFILE2_PHASE_NAMEGRAFT_COPY = 6,
    }

    const uint COPY_FILE_FAIL_IF_EXISTS = 0x00000001;
    const uint COPY_FILE_NO_BUFFERING = 0x00001000;

    const int S_OK = 0;

    static Task CopyFileAsync(string source, string destination, bool failIfExists = false, bool noBuffering = true)
    {
        var completionSource = new TaskCompletionSource<bool>();

        var callback = (ref COPYFILE2_MESSAGE message, IntPtr context) =>
        {
            if (message.Type == COPYFILE2_MESSAGE_TYPE.COPYFILE2_CALLBACK_ERROR)
            {
                var errorInfo = message.Info.Error;
                completionSource.TrySetException(new IOException($"Failed to copy file from '{source}' to '{destination}'. Phase: {errorInfo.CopyPhase}, HRESULT: 0x{errorInfo.hrFailure:X8}"));
                return COPYFILE2_MESSAGE_ACTION.COPYFILE2_PROGRESS_CANCEL;
            }

            return COPYFILE2_MESSAGE_ACTION.COPYFILE2_PROGRESS_CONTINUE;
        };

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
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

                var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);

                var parameters = new COPYFILE2_EXTENDED_PARAMETERS
                {
                    dwSize = (uint)Marshal.SizeOf<COPYFILE2_EXTENDED_PARAMETERS>(),
                    dwCopyFlags = copyFlags,
                    pfCancel = IntPtr.Zero,
                    pProgressRoutine = callbackPtr,
                    pvCallbackContext = IntPtr.Zero
                };

                var result = CopyFile2(source, destination, ref parameters);

                if (result == S_OK)
                {
                    completionSource.TrySetResult(true);
                }
                else
                {
                    completionSource.TrySetException(new IOException($"Failed to copy file from '{source}' to '{destination}'. HRESULT: 0x{result:X8}", Marshal.GetExceptionForHR(result)));
                }

                GC.KeepAlive(callback);
            }
            catch (Exception exception)
            {
                completionSource.TrySetException(exception);
            }
        });

        return completionSource.Task;
    }
}
