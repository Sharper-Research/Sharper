namespace RtaNarrowingDemo;

interface IWorker
{
    void Run();
}

abstract class WorkerBase
{
    public virtual void Tick() { }
}

class WorkerA : WorkerBase, IWorker
{
    public override void Tick() { }
    public void Run() => Tick();
}

class WorkerB : WorkerBase, IWorker
{
    public override void Tick() { }
    public void Run() => Tick();
}

class Program
{
    static void Main()
    {
        IWorker worker = new WorkerA();
        worker.Run(); // Interface dispatch should narrow to WorkerA.Run

        WorkerBase b = worker as WorkerBase ?? new WorkerA();
        b.Tick(); // Virtual dispatch should narrow to WorkerA.Tick
    }
}

