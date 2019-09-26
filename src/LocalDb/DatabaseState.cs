class DatabaseState
{
    public bool DataFileExists { get; }
    public bool LogFileExists { get; }
    public string? DbDataFileName { get; }
    public string? DbLogFileName { get; }

    public DatabaseState(bool dataFileExists, bool logFileExists, string? dbDataFileName, string? dbLogFileName)
    {
        DataFileExists = dataFileExists;
        LogFileExists = logFileExists;
        DbDataFileName = dbDataFileName;
        DbLogFileName = dbLogFileName;
    }
}