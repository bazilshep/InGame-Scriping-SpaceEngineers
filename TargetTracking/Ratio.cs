using System;

namespace SpaceEngineersIngameScript.TargetTracking
{

    public struct Ratio
    {
        public Ratio(long n, long d) { num = n; den = d; }
        public readonly long num;
        public readonly long den;
        public static implicit operator Double(Ratio x)
        {
            return (double)x.num / x.den;
        }
    }

}
