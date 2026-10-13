namespace LambdaLocalFunctionDemo;

class Program
{
    static void Main()
    {
        void LocalWork()
        {
        }

        LocalWork();

        Action lambdaWork = () =>
        {
            int value = 1;
            _ = value + 1;
        };

        lambdaWork();
    }

    static void NeverCalled()
    {
    }
}

