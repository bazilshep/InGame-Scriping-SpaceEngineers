using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{
    /// <summary>
    /// Class for clipping 3d lines to the unit cube.
    /// </summary>
    public static class lineclip3
    {
        const int INSIDE = 0; // 0000                      
        const int LEFT = 1;   // 0001                      
        const int RIGHT = 2;  // 0010                      
        const int BOTTOM = 4; // 0100                      
        const int TOP = 8;    // 1000                      
        const int IN = 16;
        const int OUT = 32;

        static readonly public Vector3 UB = Vector3.One;
        static readonly public Vector3 LB = -Vector3.One;

        static public bool clip(ref Vector3 a, ref Vector3 b)
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
                    float x = 0.0f;
                    float y = 0.0f;
                    float z = 0.0f;
                    float s = 1.0f;
                    Vector3 d = b - a;

                    // At least one endVector2I is outside the clip rectangle; pick it.                      
                    int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;

                    // Now find the intersection Vector2I;                      
                    // use formulas y = y0 + slope * (x - x0), x = x0 + (1 / slope) * (y - y0)                      
                    if ((outcodeOut & TOP) != 0)
                    {           // Vector2I is above the clip rectangle                      
                        s = (UB.Y - a.Y) / d.Y;
                        x = a.X + d.X * s;
                        z = a.Z + d.Z * s;
                        y = UB.Y;
                    }
                    else if ((outcodeOut & BOTTOM) != 0)
                    { // Vector2I is below the clip rectangle                      
                        s = (LB.Y - a.Y) / d.Y;
                        x = a.X + d.X * s;
                        z = a.Z + d.Z * s;
                        y = LB.Y;
                    }
                    else if ((outcodeOut & RIGHT) != 0)
                    {  // Vector2I is to the right of clip rectangle                      
                        s = (UB.X - a.X) / d.X;
                        y = a.Y + d.Y * s;
                        z = a.Z + d.Z * s;
                        x = UB.X;
                    }
                    else if ((outcodeOut & LEFT) != 0)
                    {   // Vector2I is to the left of clip rectangle                      
                        s = (LB.X - a.X) / d.X;
                        y = a.Y + d.Y * s;
                        z = a.Z + d.Z * s;
                        x = LB.X;
                    }
                    else if ((outcodeOut & OUT) != 0)
                    {
                        s = (UB.Z - a.Z) / d.Z;
                        z = a.X + d.X * s;
                        y = a.Y + d.Y * s;
                        z = UB.Z;
                    }
                    else if ((outcodeOut & IN) != 0)
                    {
                        s = (LB.Z - a.Z) / d.Z;
                        x = a.X + d.X * s;
                        y = a.Y + d.Y * s;
                        z = LB.Z;
                    }

                    // Now we move outside Vector2I to intersection Vector2I to clip                      
                    // and get ready for next pass.                      
                    if (outcodeOut == outcode0)
                    {
                        a.X = x;
                        a.Y = y;
                        a.Z = z;
                        outcode0 = ComputeOutCode(a);
                    }
                    else
                    {
                        b.X = x;
                        b.Y = y;
                        b.Z = z;
                        outcode1 = ComputeOutCode(b);
                    }
                }

            }
            return accept;
        }

        static private int ComputeOutCode(Vector3 p)
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
            if (p.Z < LB.Z)           // below the clip window                      
                code |= IN;
            else if (p.Z > UB.Z)      // above the clip window                      
                code |= OUT;

            return code;
        }
    }
    
}

