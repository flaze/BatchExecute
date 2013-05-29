using System;
namespace BatchExecute
{
    public static class Extensions
    {
        public static int Repeat(this int v, int count)
        {
            return (int)(v - ((Math.Ceiling(v / (double)count) - 1) * count));
        }
    }
}
