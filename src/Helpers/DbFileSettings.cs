public class DbFileSettings
{
    public string Name { get; }
    public string Filename { get; }

    public DbFileSettings(string name, string filename)
    {
        Name = name;
        Filename = filename;
    }
}