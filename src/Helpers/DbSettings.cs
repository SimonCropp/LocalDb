using System.Collections.Generic;

public class DbSettings
{
    public List<DbFileSettings> Files { get; }

    public DbSettings(List<DbFileSettings> files)
    {
        Files = files;
    }
}