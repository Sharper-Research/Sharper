using System;

namespace TargetApp
{
    internal static class Worker
    {
        public static void RunLoop1(int check, int[] buffer)
        {
            int i = 0;
            while (i < check)
            {
                // Deliberate bug: no bounds check
                buffer[i] = i + 1;

                i++;
            }

            Console.WriteLine("Loop 1 Finished");
        }

        public static void RunLoop2(int[] buffer, int startIndex)
        {
            int i = startIndex;
            while (i >= 0)
            {
                // Deliberate bug: no bounds check
                buffer[i] = i + 1;

                i--;
            }
        }
    }
}
