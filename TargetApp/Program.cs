using System;

namespace TargetApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Simulate input
            int check = 11; 
            int i = 0;
            int[] buf1 = new int[10];
            int[] buf2 = new int[8];

            // ---------------------------------------------------------
            // BUGGY LOOP from Abhishek's PDF
            // ---------------------------------------------------------
            while (i < check) 
            {
                // This is where it would crash if we don't have protection
                if (i < buf1.Length) 
                    buf1[i] = i + 1;
                
                i++;
            }

            Console.WriteLine("Loop 1 Finished");

            while (i >= 0)
            {
               if (i < buf2.Length)
                   buf2[i] = i + 1; 
               i--;
            }
        }
    }
}