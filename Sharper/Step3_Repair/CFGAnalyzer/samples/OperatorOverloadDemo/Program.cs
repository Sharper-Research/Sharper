namespace OperatorOverloadDemo;

public readonly struct Money
{
    public Money(int amount)
    {
        Amount = amount;
    }

    public int Amount { get; }

    public static Money operator +(Money left, Money right)
    {
        return new Money(left.Amount + right.Amount);
    }

    public static Money operator -(Money value)
    {
        return new Money(-value.Amount);
    }

    public static bool operator ==(Money left, Money right)
    {
        return left.Amount == right.Amount;
    }

    public static bool operator !=(Money left, Money right)
    {
        return !(left == right);
    }

    public static implicit operator Money(int amount)
    {
        return new Money(amount);
    }

    public static explicit operator int(Money value)
    {
        return value.Amount;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money other && this == other;
    }

    public override int GetHashCode()
    {
        return Amount;
    }
}

public static class Program
{
    public static void Main()
    {
        Money first = 10;
        Money second = 5;

        var sum = first + second;
        var negated = -sum;
        var areEqual = negated == second;
        var areDifferent = first != second;
        var numeric = (int)sum;
        double widened = numeric;

        Console.WriteLine($"{numeric}:{areEqual}:{areDifferent}:{widened}");
    }
}
