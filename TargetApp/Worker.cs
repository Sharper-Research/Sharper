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
                buffer[i] = i + 1;
                i--;
            }
        }

        // Intra-method aliasing test: alias references the same buffer
        public static void RunLoop3(int[] data)
        {
            int[] alias = data;
            alias[data.Length] = 99; // OOB via alias
        }
    }
}
