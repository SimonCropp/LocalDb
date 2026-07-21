/// <summary>
/// Records which LocalDB instances were created by this library.
/// <para>
/// LocalDB instances are shared across everything on the machine that uses LocalDB, and nothing
/// in an instance says what created it. Without a record, reclaiming an instance means guessing,
/// so a marker is written into the directory LocalDB keeps for each instance this library starts.
/// The ongoing cleanup only reclaims marked instances, and so can never remove one belonging to
/// anything else.
/// </para>
/// <para>
/// The marker lives in the instance directory rather than a registry of its own, so it is removed
/// with the instance and cannot drift out of sync. It survives the temp directory being cleared,
/// which is what orphans an instance in the first place.
/// </para>
/// </summary>
static class InstanceMarker
{
    const string fileName = ".localdbwrapper";

    /// <summary>
    /// Records an instance as created by this library. Best effort: a machine where the marker
    /// cannot be written still works, those instances are just never reclaimed automatically.
    /// </summary>
    public static void Mark(string instanceName)
    {
        try
        {
            var directory = DirectoryFinder.FindInstance(instanceName);
            // LocalDB owns this directory and creates it with the instance
            if (!Directory.Exists(directory))
            {
                return;
            }

            var marker = Path.Combine(directory, fileName);
            if (!File.Exists(marker))
            {
                File.WriteAllText(marker, string.Empty);
            }
        }
        catch (Exception exception)
        {
            LocalDbLogging.LogIfVerbose($"Failed to mark instance: {instanceName}. {exception.Message}");
        }
    }

    public static bool IsMarked(string instanceDirectory) =>
        File.Exists(Path.Combine(instanceDirectory, fileName));

    /// <summary>
    /// Whether model.mdf has been shrunk below the size a fresh instance has.
    /// <para>
    /// Only used for the one time pass over instances that predate marking. This library shrinks
    /// model when it creates an instance, and nothing else does, so a smaller model.mdf means the
    /// instance was created by it. Instances left at the default size are not matched, so the
    /// pass can never remove an instance belonging to anything else.
    /// </para>
    /// </summary>
    public static bool HasShrunkModel(string instanceDirectory)
    {
        var model = Path.Combine(instanceDirectory, "model.mdf");
        if (!File.Exists(model))
        {
            return false;
        }

        var length = new FileInfo(model).Length;
        return length > 0 &&
               length < 8 * 1024 * 1024;
    }
}
