static class StringExtensions
{
    public static string IndentLines(this string input)
    {
        var indent = "    ";
        return $"{indent}{input.Replace("\n", $"\n{indent}")}";
    }
}