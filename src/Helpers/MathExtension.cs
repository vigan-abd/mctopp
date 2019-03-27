using System;

namespace MCTOPP.Helpers
{
    public class MathExtension
    {
        public static float Euclidean(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
    }
}