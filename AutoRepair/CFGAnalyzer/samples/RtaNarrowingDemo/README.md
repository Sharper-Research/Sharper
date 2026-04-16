# RTA Narrowing Demo

This sample is used to validate `RapidTypeAnalysisCallResolver`.

## Idea
- Two classes (`WorkerA`, `WorkerB`) implement `IWorker` and override `WorkerBase.Tick`.
- `Main` only instantiates `WorkerA`.
- Interface call `worker.Run()` and virtual call `b.Tick()` should resolve to `WorkerA` targets.

## Run

```zsh
dotnet build samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj
dotnet run --project CFGAnalyzer.csproj -- samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj
```

## Automated check

- macOS/Linux:

```zsh
./scripts/verify-rta-demo.sh
```

- Windows:

```bat
scripts\verify-rta-demo.bat
```

The scripts run `samples/RtaNarrowingDemo/EdgeProbe/EdgeProbe.csproj`, which calls `ProjectAnalyzer` directly and prints the resolved `Main` callees.
They validate both:
- `samples/RtaNarrowingDemo/RtaNarrowingDemo.csproj`
- `samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj`

## What to check
Look for call edges/call sites from `RtaNarrowingDemo.Program.Main()` that include:
- `RtaNarrowingDemo.WorkerA.Run()`
- `RtaNarrowingDemo.WorkerA.Tick()`

and exclude:
- `RtaNarrowingDemo.WorkerB.Run()`
- `RtaNarrowingDemo.WorkerB.Tick()`

