using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

    /// <summary>
    /// Class for clipping 2d lines to the unit square.
    /// </summary>
    public static class lineclip2
    {
        const int INSIDE = 0; // 0000                      
        const int LEFT = 1;   // 0001                      
        const int RIGHT = 2;  // 0010                      
        const int BOTTOM = 4; // 0100                      
        const int TOP = 8;    // 1000                      

        static public readonly Vector2 UB = Vector2.One;
        static public readonly Vector2 LB = -Vector2.One;

        static public bool clip(ref Vector2 a, ref Vector2 b)
        {
            int outcode0 = ComputeOutCode(a);
            int outcode1 = ComputeOutCode(b);
            bool accept = false;

            while (true)
            {
                if ((outcode0 | outcode1) == 0)
                { // Bitwise OR is 0. Trivially accept and get out of loop                      
                    accept = true;
                    break;
                }
                else if ((outcode0 & outcode1) != 0)
                { // Bitwise AND is not 0. Trivially reject and get out of loop                      
                    break;
                }
                else
                {
                    // failed both tests, so calculate the line segment to clip                      
                    // from an outside Vector2I to an intersection with clip edge                      
                    float x = 0;
                    float y = 0;

                    // At least one endVector2I is outside the clip rectangle; pick it.                      
                    int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                    // Now find the intersection Vector2I;                      
                    // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)                      
                    if ((outcodeOut & TOP) != 0)
                    {           // Vector2I is above the clip rectangle                      
                        x = a.X + (b.X - a.X) * (UB.Y - a.Y) / (b.Y - a.Y);
                        y = UB.Y;
                    }
                    else if ((outcodeOut & BOTTOM) != 0)
                    { // Vector2I is below the clip rectangle                      
                        x = a.X + (b.X - a.X) * (LB.Y - a.Y) / (b.Y - a.Y);
                        y = LB.Y;
                    }
                    else if ((outcodeOut & RIGHT) != 0)
                    {  // Vector2I is to the right of clip rectangle                      
                        y = a.Y + (b.Y - a.Y) * (UB.X - a.X) / (b.X - a.X);
                        x = UB.X;
                    }
                    else if ((outcodeOut & LEFT) != 0)
                    {   // Vector2I is to the left of clip rectangle                      
                        y = a.Y + (b.Y - a.Y) * (LB.X - a.X) / (b.X - a.X);
                        x = LB.X;
                    }

                    // Now we move outside Vector2I to intersection Vector2I to clip                      
                    // and get ready for next pass.                      
                    if (outcodeOut == outcode0)
                    {
                        a.X = x;
                        a.Y = y;
                        outcode0 = ComputeOutCode(a);
                    }
                    else
                    {
                        b.X = x;
                        b.Y = y;
                        outcode1 = ComputeOutCode(b);
                    }
                }

            }
            return accept;
        }

        static private int ComputeOutCode(Vector2 p)
        {
            int code;

            code = INSIDE;          // initialised as being inside of [[clip window]]                      

            if (p.X < LB.X)           // to the left of clip window                      
                code |= LEFT;
            else if (p.X > UB.X)      // to the right of clip window                      
                code |= RIGHT;
            if (p.Y < LB.Y)           // below the clip window                      
                code |= BOTTOM;
            else if (p.Y > UB.Y)      // above the clip window                      
                code |= TOP;

            return code;
        }
    }

    }
