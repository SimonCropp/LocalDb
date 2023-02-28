namespace EfLocalDb;

class MoreThanOneException : Exception
{
    public object[] Keys { get; }
    public List<object> Results { get; }

    public MoreThanOneException(object[] keys, List<object> results)
    {
        Keys = keys;
        Results = results;
    }

    public override string Message =>
        $"""
            More than one record found.
              * Keys: {string.Join(", ", Keys)}
              * Types: {string.Join(", ", Results.Select(_ => _.GetType().Name))}
            """;
}