public static class Program
{
    public static void ProcessBuffers(int check, int i)
    {
        int[] buf1 = new int[5];
        int[] buf2 = new int[2];
        while ((i < check) && buf1 != null && i >= 0 && i < buf1.Length)
        {
            buf1[i] = i + 1;
            ++i;
        }
        while ((i >= 0) && buf2 != null && i >= 0 && i < buf2.Length)
        {
            buf2[i] = i + 1;
            --i;
        }
    }
}