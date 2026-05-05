namespace ExtensionMethodDemo;

public static class TextExtensions
{
    public static string WrapInBrackets(this string value)
    {
        return BuildWrapped(value);
    }

    public static string Duplicate(this string value)
    {
        return Combine(value, value);
    }

    private static string BuildWrapped(string value)
    {
        return "[" + value + "]";
    }

    private static string Combine(string left, string right)
    {
        return left + right;
    }
}

public static class Program
{
    public static void Main()
    {
        var raw = "cfg";
        var wrapped = raw.WrapInBrackets();
        var duplicated = wrapped.Duplicate();
        Console.WriteLine(duplicated);
    }
}

