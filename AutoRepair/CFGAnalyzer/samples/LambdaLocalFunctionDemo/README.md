# Lambda and Local Function Demo

This sample validates call resolution for:
- local functions (`LocalFunctionStatementSyntax` / `ILocalFunctionOperation`)
- lambdas (`AnonymousFunctionExpressionSyntax` / `IAnonymousFunctionOperation`)

## Run

```zsh
dotnet build samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj
dotnet run --project samples/RtaNarrowingDemo/EdgeProbe/EdgeProbe.csproj -- samples/LambdaLocalFunctionDemo/LambdaLocalFunctionDemo.csproj
```

## What to check
The probe output should include generated callee symbols from `Main` for:
- local function (`g__` generated name)
- lambda (`b__` generated name)

and exclude:
- `NeverCalled`

