#if EF
namespace EfLocalDb;
#else
namespace LocalDb;
#endif

public struct ExistingTemplate
{
    public string DataPath { get; }
    public string LogPath { get; }

    public ExistingTemplate(string dataPath, string logPath)
    {
        Ensure.NotNullOrWhiteSpace(dataPath);
        Ensure.NotNullOrWhiteSpace(logPath);
        DataPath = dataPath;
        LogPath = logPath;
    }
}