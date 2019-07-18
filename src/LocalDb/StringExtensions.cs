static class StringExtensions
{
    public static string IndentLines(this string input)
    {
        const string indent = "    ";
        return $"{indent}{input.Replace("\n", $"\n{indent}")}";
    }
}