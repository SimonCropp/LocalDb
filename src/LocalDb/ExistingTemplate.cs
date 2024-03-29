﻿#if EF
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
        Guard.AgainstNullWhiteSpace(dataPath);
        Guard.AgainstNullWhiteSpace(logPath);
        DataPath = dataPath;
        LogPath = logPath;
    }
}