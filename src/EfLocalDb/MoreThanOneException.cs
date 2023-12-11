namespace EfLocalDb;

class MoreThanOneException(object[] keys, List<object> results) :
    Exception
{
    public object[] Keys { get; } = keys;
    public List<object> Results { get; } = results;

    public override string Message =>
        $"""
            More than one record found.
              * Keys: {string.Join(", ", Keys)}
              * Types: {string.Join(", ", Results.Select(_ => _.GetType().Name))}
            """;
}