using System;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

    public struct BresenhamLineGenerator: IComparable<BresenhamLineGenerator>
    {
        int t;
        Vector2I delta;
        int pdx, pdy;
        int ddx, ddy;
        int es, el;
        int err;

        Vector2I p;
        public Vector2I Pt;

        public BresenhamLineGenerator(Vector2I start, Vector2I stop)
        {
            delta = stop - start;

            int incrementX = Math.Sign(delta.X);
            int incrementY = Math.Sign(delta.Y);
            if (delta.X < 0) delta.X = -delta.X;
            if (delta.Y < 0) delta.Y = -delta.Y;

            if (delta.X > delta.Y)
            {
                pdx = incrementX; pdy = 0;
                ddx = incrementX; ddy = incrementY;
                es = delta.Y; el = delta.X;
            }
            else
            {
                pdx = 0; pdy = incrementY;
                ddx = incrementX; ddy = incrementY;
                es = delta.X; el = delta.Y;
            }

            err = el / 2;
            t = 0;
            p = start;
            Pt = p;
        }
        public Boolean Advance()
        {

            err -= es;
            if (err < 0)
            {
                err += el;
                p.X += ddx;
                p.Y += ddy;
            }
            else
            {
                p.X += pdx;
                p.Y += pdy;
            }
            Pt = p;

            t++;
            
            return t-1 < el;
        }

        //compares line generators by angle (me cross other) compared to 0
        int IComparable<BresenhamLineGenerator>.CompareTo(BresenhamLineGenerator other)
        {
            return ((long)other.delta.Y * delta.X - (long)other.delta.X * delta.Y).CompareTo(0);
        }
    }

}

