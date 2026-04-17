// DelegateDemo – exercises intraprocedural delegate resolution scenarios.

// ── 1. Named delegate type via local variable ─────────────────────────────
static void Greet(string name) => Console.WriteLine($"Hello, {name}!");
static void Farewell(string name) => Console.WriteLine($"Goodbye, {name}!");

Action<string> greeter = Greet;
greeter("Alice");

// ── 2. Delegate swapped on a branch ──────────────────────────────────────
bool useFormal = true;
Action<string> pick = useFormal ? Greet : Farewell;
pick("Bob");

// ── 3. Delegate stored in a field of a helper class ───────────────────────
var runner = new TaskRunner();
runner.Run();

// ── 4. Lambda delegate ────────────────────────────────────────────────────
Func<int, int> doubler = x => x * 2;
Console.WriteLine(doubler(21));

// ── 5. Passing a delegate to a method ────────────────────────────────────
ApplyAndPrint(42, x => x + 8);

static void ApplyAndPrint(int value, Func<int, int> transform)
{
    var result = transform(value);
    Console.WriteLine(result);
}

class TaskRunner
{
    private readonly Action _action;

    public TaskRunner()
    {
        _action = DoWork;
    }

    public void Run() => _action();

    private static void DoWork() => Console.WriteLine("Working…");
}

