namespace FunctionPointerDemo;

public static class Operations
{
    public static int Add(int x, int y) => x + y;

    public static int Multiply(int x, int y) => x * y;

    public static int Square(int x) => x * x;
}

public static class Program
{
    public static unsafe void Main()
    {
        // Assign method address to a function pointer
        delegate* <int, int, int> binOp = &Operations.Add;

        // Invoke through the pointer
        var sum = binOp(2, 3);

        // Reassign the pointer
        binOp = &Operations.Multiply;
        var product = binOp(4, 5);

        // Unary pointer (single argument)
        delegate* <int, int> unaryOp = &Operations.Square;
        var squared = unaryOp(6);

        Console.WriteLine($"{sum}:{product}:{squared}");
    }
}

