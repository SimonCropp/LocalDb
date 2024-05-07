class DatabaseState(bool dataFileExists, bool logFileExists, string? dbDataFileName, string? dbLogFileName)
{
    public bool DataFileExists { get; } = dataFileExists;
    public bool LogFileExists { get; } = logFileExists;
    public string? DbDataFileName { get; } = dbDataFileName;
    public string? DbLogFileName { get; } = dbLogFileName;
}