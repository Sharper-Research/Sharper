namespace GenericsDemo;

class Program
{
    static void Main()
    {
        PrintValue(42);
        PrintValue("hello");

        var box = new Box<int>(10);
        box.Update(20);

        var workflow = new StringBoxWorkflow();
        workflow.Run();
    }

    static void PrintValue<T>(T value) where T : notnull
    {
        Consume(value);
    }

    static void Consume<T>(T value) where T : notnull
    {
        _ = value.ToString();
    }
}

class Box<T> where T : notnull
{
    private T _value;

    public Box(T value)
    {
        _value = value;
    }

    public void Update(T value)
    {
        _value = value;
        Log(_value);
    }

    private static void Log(T value)
    {
        _ = value.ToString();
    }
}

class StringBoxWorkflow
{
    private readonly Box<string> _box = new("seed");

    public void Run()
    {
        _box.Update("next");
        MirrorIntoLocalBox(_box);
    }

    private static void MirrorIntoLocalBox(Box<string> source)
    {
        var local = new Box<string>("local");
        local.Update(source.ToString() ?? "fallback");
    }
}

