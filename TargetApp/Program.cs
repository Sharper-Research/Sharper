using System;

namespace TargetApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Simulate input
            int check = 11;
            int[] buf1 = new int[10];
            int[] buf2 = new int[8];

            // ---------------------------------------------------------
            // BUGGY LOOP from Abhishek's PDF
            // ---------------------------------------------------------
            Worker.RunLoop1(check, buf1);
            Worker.RunLoop2(buf2, check);

            // Intra-method alias test
            Worker.RunLoop3(buf1);

            buf1[buf1.Length] = 42;
        }
    }
}
