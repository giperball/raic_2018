using System;

namespace ConsoleApp1.Common
{
    public static class DoubleExtension 
    {
        public static bool AlmostEqualTo(this double value1, double value2, double eps = 0.01D)
        {
            return Math.Abs(value1 - value2) < eps; 
        }
    }
}