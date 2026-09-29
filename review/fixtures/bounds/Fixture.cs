public static class Fixture
{
    public static int Read(int[] array, int index, int next, bool condition)
    {
        if (condition)
            index = next;
        else
            index = 0;
        return array[index];
    }
}