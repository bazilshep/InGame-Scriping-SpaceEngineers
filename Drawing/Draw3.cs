using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

    /// <summary>
    /// Implements drawing a wireframe 3d scene.
    /// </summary>
    public class Draw3
    {
        private Canvas canvas;

        private Vector2I drawCenter;
        private Vector2I drawSize;
        private Vector2I drawLB;
        private Vector2I drawUB;

        protected Matrix Camera;

        protected List<Matrix> ProjStack;

        protected Matrix proj;

        public Draw3(Canvas d, Vector2I LB, Vector2I UB)
        {
            canvas = d;
            drawLB = LB;
            drawUB = UB;
            drawCenter = (LB + UB) / 2;
            drawSize = (UB + LB);

            Camera = Matrix.CreatePerspectiveFovRhInfinite((float)Math.PI * 55f / 180f, 1, .1f);

            ProjStack = new List<Matrix>(16);
            proj = Camera;
        }

        public void color(byte r, byte g, byte b) { canvas.setColor(r, g, b); }

        public void color(char c)
        {
            canvas.setColor(c);
        }

        private bool P3ToNDC(Vector3 p3, ref Vector3 ndc)
        {
            Vector4 p4 = Vector4.Transform(p3, proj);
            ndc.X = p4.X / p4.W;
            ndc.Y = p4.Y / p4.W;
            ndc.Z = p4.Z / p4.W;
            return ndc.X < 1.0f & ndc.X > -1.0f &
                    ndc.Y < 1.0f & ndc.Y > -1.0f &
                    ndc.Z < 1.0f & ndc.Z > -1.0f;
        }

        private bool P3ToSC(Vector3 p3, ref Vector3 ndc, ref Vector2I sc)
        {
            bool inside = P3ToNDC(p3, ref ndc);
            NDCToSC(ndc, ref sc);
            return inside;
        }

        private bool NDCToSC(Vector3 ndc, ref Vector2I sc)
        {
            sc.X = (int)(ndc.X * drawSize.X) + drawCenter.X;
            sc.Y = (int)(ndc.Y * drawSize.Y) + drawCenter.Y;
            return ndc.X < 1.0f & ndc.X > -1.0f &
                    ndc.Y < 1.0f & ndc.Y > -1.0f &
                    ndc.Z < 1.0f & ndc.Z > -1.0f;
        }

        public void line(Vector3 a, Vector3 b)
        {
            Vector3 aNDC = new Vector3();
            P3ToNDC(a, ref aNDC);
            Vector3 bNDC = new Vector3();
            P3ToNDC(b, ref bNDC);
            Vector3 bNDC_unclipped = bNDC;
            if (lineclip3.clip(ref aNDC, ref bNDC))
            {
                Vector2I aSC = new Vector2I();
                NDCToSC(aNDC, ref aSC);
                Vector2I bSC = new Vector2I();
                NDCToSC(bNDC, ref bSC);

                Vector2 depthgrad = new Vector2(bSC.X - aSC.X, bSC.Y - aSC.Y);
                depthgrad = depthgrad * (bNDC.Z - aNDC.Z) / depthgrad.LengthSquared();
                canvas.DepthGrad = depthgrad;
                canvas.DepthOffset = aNDC.Z - canvas.DepthGrad.X * aSC.X - canvas.DepthGrad.Y * aSC.Y;

                canvas.move(aSC);
                canvas.line(bSC);
            }
            Vector2I bSC_unclipped = new Vector2I();
            NDCToSC(bNDC, ref bSC_unclipped);
            canvas.move(bSC_unclipped);
        }

        private static Vector2 depthgradient(ref Vector2I sc1, ref float d1, ref Vector2I sc2, ref float d2)
        {
            Vector2 depthgrad = sc2 - sc1;
            return depthgrad * (d2 - d1) / depthgrad.LengthSquared();
        }

        public void rect(BoundingBox box)
        {
            Vector3 ub = box.Max;
            Vector3 lb = box.Min;

            Vector3 s = (ub - lb) / 2;
            Vector3 c = (ub + lb) / 2;

            Vector3 x = Vector3.UnitX * s;
            Vector3 y = Vector3.UnitY * s;
            Vector3 z = Vector3.UnitZ * s;

            //edges parallel to x                      
            line(c - z - y - x, c - z - y + x);
            line(c + z - y - x, c + z - y + x);
            line(c - z + y - x, c - z + y + x);
            line(c + z + y - x, c + z + y + x);

            //edges parallel to y                      
            line(c - x - z - y, c - x - z + y);
            line(c + x - z - y, c + x - z + y);
            line(c - x + z - y, c - x + z + y);
            line(c + x + z - y, c + x + z + y);

            //edges parallel to z                      
            line(c - x - y - z, c - x - y + z);
            line(c + x - y - z, c + x - y + z);
            line(c - x + y - z, c - x + y + z);
            line(c + x + y - z, c + x + y + z);

        }

        public bool point(Vector3 p)
        {
            Vector2I sc = new Vector2I();
            Vector3 ndc = new Vector3();
            if (P3ToSC(p, ref ndc, ref sc))
            {
                canvas.DepthGrad = Vector2.Zero;
                canvas.DepthOffset = ndc.Z;
                canvas.set(sc);
                return true;
            }
            else
            {
                canvas.set(sc);
                return false;
            }
        }

        public bool move(Vector3 p)
        {
            Vector2I sc = new Vector2I();
            Vector3 ndc = new Vector3();
            bool inside = P3ToSC(p, ref ndc, ref sc);
            canvas.DepthGrad = Vector2.Zero;
            canvas.DepthOffset = ndc.Z;
            canvas.move(sc);
            return inside;
        }
        
        /// <summary>
        /// Draws a 2d circle to the screen at the given 3d position. Note:
        /// this can easily cause "Script Too Complex" error if drawing
        /// a very large circle.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="r">Circle size in pixels.</param>
        /// <returns></returns>
        public bool circle(Vector3 p, int r)
        {
            Vector2I sc = new Vector2I();
            Vector3 ndc = new Vector3();
            bool inside = P3ToSC(p, ref ndc, ref sc);
            canvas.DepthGrad = Vector2.Zero;
            canvas.DepthOffset = ndc.Z;
            canvas.move(sc);
            if (inside) { canvas.circle(r); }
            return inside;
        }

        public void view(Matrix view)
        {
            proj = view * Camera;
        }

        public void viewPush(Matrix trf)
        {
            ProjStack.Add(proj);
            proj = trf * proj;
        }

        public void viewPop()
        {
            proj = ProjStack[ProjStack.Count - 1];
            ProjStack.RemoveAt(ProjStack.Count - 1);
        }

    }
    
}

