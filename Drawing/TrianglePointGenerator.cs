using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

    public struct TrianglePointGenerator
    {
        Vector2I v1;
        Vector2I v2;
        Vector2I v3;

        public TrianglePointGenerator(Vector2I a, Vector2I b, Vector2I c)
        {
            v1 = a;
            v2 = b;
            v3 = c;
            //sort by x coordinate
            if (v1.X > v2.X) swap(ref v1, ref v2);
            if (v1.X > v3.X) swap(ref v1, ref v3);
            if (v2.X > v3.X) swap(ref v2, ref v3);

            longleg = new BresenhamLineGenerator(v1, v3);
            shortleg = new BresenhamLineGenerator(v1, v2);
            longlegYSmaller = ((long)(v1.Y - v3.Y) * (v2.X - v1.X) + (long)(v3.X - v1.X) * (v2.Y - v1.Y)) > 0;
            lastShortLeg = false;

            p = v1;
            start = v1;
            stop = v1;
            line = p.X;

            Pt = v1;
            
        }

        BresenhamLineGenerator shortleg;
        BresenhamLineGenerator longleg;
        bool longlegYSmaller;
        bool lastShortLeg;

        int line;
        Vector2I start;
        Vector2I stop;

        Vector2I p;
        public Vector2I Pt;
        public bool Advance()
        {
            p.Y++;
            bool advancing = true;
            if (p.Y > stop.Y)
                advancing = advanceLine();
            Pt = p;
            return advancing || (p.X==v3.X & p.Y==v3.Y);
        }
        private bool advanceLine()
        {
            line++;
            bool advancing;
            if (longlegYSmaller) {
                do
                {
                    if (longleg.Pt.Y < start.Y | longleg.Pt.X > start.X) start = longleg.Pt;
                    advancing = longleg.Advance();
                } while (advancing & longleg.Pt.X < line);

                do
                {
                    if (shortleg.Pt.Y > stop.Y | shortleg.Pt.X > stop.X) stop = shortleg.Pt;
                    advancing = advanceShortLeg();
                } while (advancing & shortleg.Pt.X < line);
            } else
            {
                do
                {
                    if (longleg.Pt.Y > stop.Y | longleg.Pt.X > stop.X) stop = longleg.Pt;
                    advancing = longleg.Advance();
                } while (advancing & longleg.Pt.X < line);

                do
                {
                    if (shortleg.Pt.Y < start.Y | shortleg.Pt.X > start.X) start = shortleg.Pt;
                    advancing = advanceShortLeg();
                } while (advancing & shortleg.Pt.X < line);
            }
            p = start;
            return advancing;
        }

        private bool advanceShortLeg()
        {
            bool advance = shortleg.Advance();
            if (!advance & !lastShortLeg)
            {
                lastShortLeg = true;
                shortleg = new BresenhamLineGenerator(v2, v3);
                advance = true;
            }
            return advance;
        }

        static void swap(ref Vector2I a, ref Vector2I b) { var tmp = a; a = b; b = tmp; }
    }
        
}
