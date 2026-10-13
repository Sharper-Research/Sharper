public static class Fixture
{
    public static int Read(int[] array, int index, int next, bool condition)
    {
        if (condition)
            index = next;
        else
            index = 0;
        {
            if (array is null || index < 0 || index >= array.Length)
                return default; return array[index];
        }
    }
}