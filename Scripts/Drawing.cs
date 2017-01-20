using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using VRage.Game; // VRage.Game.dll
using System.Text;
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using Sandbox.Game.EntityComponents; // Sandbox.Game.dll
using VRage.Game.Components; // VRage.Game.dll
using VRage.Collections; // VRage.Library.dll
using VRage.Game.ObjectBuilders.Definitions; // VRage.Game.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll
using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using SpaceEngineersIngameScript.Scripts;

namespace SpaceEngineersIngameScript.Scripts
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

    /// <summary>
    /// Implements a vector drawing canvas. Also implements
    /// depth buffering. Note: The ingame LCD block must use
    /// the MonospaceFont.
    ///
    /// This class contains code adapted from BaconFist's BaconDraw project.
    /// Copyright 2016 Thomas Klose <thomas@bratler.net>
    /// https://github.com/BaconFist/SpaceEngineersIngameScript
    ///
    /// </summary>
    public class Canvas
    {
        private Vector2I pos = new Vector2I(0, 0);
        public readonly int width;
        public readonly int height;
        private char c = rgb(7, 7, 7);

        private char[][] data;
        private float[,] depth;

        public Vector2 DepthGrad = Vector2.Zero;
        public float DepthOffset = 0.0f;

        public bool depthtest = false;

        public Canvas(Vector2I p)
        {
            width = p.X;
            height = p.Y;
            data = new Char[p.Y][];
            depth = new float[p.Y, p.X];
            for (int i = 0; i < height; i++)
            {
                data[i] = (new String(rgb(0,0,0), p.X)).ToCharArray();
                for (int j = 0; j < width; j++)
                {
                    depth[i, j] = 100.0f;
                }
            }
        }

        public void setColor(byte r, byte g, byte b)
        {
            c = rgb(r, g, b);
        }

        public void setColor(char color)
        {
            c = color;
        }

        public void set(Vector2I P)
        {
            set(P, true);
        }

        private void setsym(int x, int y)
        {
            set(new Vector2I(pos.X + x, pos.Y + y), false);
            set(new Vector2I(pos.X + y, pos.Y + x), false);
            set(new Vector2I(pos.X + y, pos.Y + -x), false);
            set(new Vector2I(pos.X + x, pos.Y + -y), false);
            set(new Vector2I(pos.X + -x, pos.Y + -y), false);
            set(new Vector2I(pos.X + -y, pos.Y + -x), false);
            set(new Vector2I(pos.X + -y, pos.Y + x), false);
            set(new Vector2I(pos.X + -x, pos.Y + y), false);
        }

        public void set(Vector2I p, bool move)
        {
            if (p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height)
            {
                float ptdepth = DepthGrad.X * p.X + DepthGrad.Y * p.Y + DepthOffset;
                if (depthtest || depth[p.Y, p.X] + 1e-6f > ptdepth)
                {
                    data[p.Y][p.X] = c;
                    depth[p.Y, p.X] = ptdepth;
                }
            }
            if (move)
            {
                pos = p;
            }
        }

        public void move(Vector2I P)
        {
            this.pos = P;
        }

        public override string ToString()
        {
            StringBuilder slug = new StringBuilder();
            for (int h = 0; h < data.Length; h++)
            {
                slug.AppendLine(new String(data[h]));
            }
            return slug.ToString();
        }

        public void clear(char background)
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    data[i][j] = background;
                    depth[i, j] = 100f;
                }
            }
        }

        public void clear(byte r, byte g, byte b)
        {
            clear(rgb(r, g, b));
        }

        public void FlushToDisplay(IMyTextPanel Panel)
        {
            Panel.WritePublicText(ToString(), false);
            Panel.ShowPublicTextOnScreen();
        }

        public static char rgb(byte r, byte g, byte b)
        {
            return (char)(0xe100 + (r << 6) + (g << 3) + b);
        }

        public void circle(int r)
        {
            int d;
            int x = r;
            d = r * -1;
            for (int y = 0; y <= x; y++)
            {
                setsym(x, y);
                d = d + 2 * y + 1;
                if (d > 0)
                {
                    d = d - 2 * x + 2;
                    x = x - 1;
                }
            }
        }

        public void triangle(Vector2I v2, Vector2I v3)
        {
            TrianglePointGenerator tri = new TrianglePointGenerator(pos, v2, v3);
            do
                set(tri.Pt);
            while (tri.Advance());
        }

        public void line(Vector2I p)
        {
            Vector2 start = new Vector2(((float)pos.X) / width, ((float)pos.Y) / height);
            Vector2 end = new Vector2(((float)p.X) / width, ((float)p.Y) / height);
            if (lineclip2.clip(ref start, ref end))
            {
                move(new Vector2I((int)(start.X * width), (int)(start.Y * height)));
                line_Unclipped(new Vector2I((int)(end.X * width), (int)(end.Y * height)));
            }
            move(p);
        }

        private void line_Unclipped(Vector2I target)
        {
            var gen = new BresenhamLineGenerator(pos, target);
            do
            {
                set(gen.Pt);
            } while (gen.Advance());
        }

        public void rect(Vector2I p)
        {
            line(new Vector2I(p.X, 0));
            line(new Vector2I(0, p.Y));
            line(new Vector2I(-p.X, 0));
            line(new Vector2I(0, -p.Y));
        }

        public char getPixel(int r, int c)
        {
            return data[r][c];
        }

#if DEBUG
        public System.Drawing.Bitmap Bitmap { get{ return canvasExtension.toBitMap(this); } }
#endif
    }

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

    public class Scene
    {
        public List<Model> models;

        public Scene()
        {
            models = new List<Model>();
        }

        public void draw(Draw3 viewer)
        {
            foreach (Model m in models)
            {
                m.draw(viewer);
            }
        }
    }

    public abstract class Model
    {
        public abstract void draw(Draw3 viewer);
    }

    public class RadarModel : Model
    {
        public float height = 1.0f;

        public List<TargetType> targets;
        public Matrix TargetTransform;
        private Matrix inv;

        public char planeColor = Canvas.rgb(4, 3, 2);
        public char aboveColor = Canvas.rgb(5, 5, 5);
        public char belowColor = Canvas.rgb(2, 2, 2);
        public char radialColor = Canvas.rgb(5, 2, 5);

        bool drawOrientation = false;

        public RadarModel()
        {
            targets = new List<TargetType>();
        }

        public override void draw(Draw3 viewer)
        {
            inv = Matrix.Invert(TargetTransform);

            viewer.color(7, 7, 7);
            drawBound(viewer);

            foreach (TargetType tgt in targets)
            {
                viewer.color(7, 7, 7);
                drawTarget(viewer, tgt);
            }
        }

        protected void drawBound(Draw3 viewer)
        {
            const int cyl_sides = 16;

            Vector3 p0 = Vector3.UnitX;
            Vector3 p1;
            viewer.color(planeColor);
            for (int i = 0; i <= cyl_sides; i++)
            {
                p1 = p0;
                float a = (float)Math.PI * i * 2.0f / cyl_sides;
                p0 = Vector3.UnitX * (float)Math.Cos(a) + Vector3.UnitY * (float)Math.Sin(a);
                viewer.line(p1, p0);
                //viewer.line(p1 + Vector3.UnitZ * height, p0 + Vector3.UnitZ * height);          
                //viewer.line(p1 - Vector3.UnitZ * height, p0 - Vector3.UnitZ * height);          
            }
            viewer.point(Vector3.Zero);
        }

        protected void drawTarget(Draw3 viewer, TargetType target)
        {

            Vector3 targ = Vector3.Transform(target.Bounds.Center, TargetTransform);
            Vector3 targXY = targ;
            targXY.Z = 0;

            Matrix pose = target.Pose * TargetTransform;

            viewer.color(radialColor);
            viewer.line(Vector3.Zero, targXY);

            viewer.color((targ.Z < 0) ? aboveColor : belowColor);
            viewer.line(targXY, targ);

            if (drawOrientation)
            {
                viewer.color(5, 5, 5);
                viewer.point(targXY);

                viewer.color(5, 0, 0);
                viewer.line(targ, targ + pose.Forward * (.1f / TargetTransform.Scale));

                viewer.color(0, 5, 0);
                viewer.line(targ, targ + pose.Left * (.1f / TargetTransform.Scale));

                viewer.color(0, 0, 4);
                viewer.line(targ, targ + pose.Up * (.1f / TargetTransform.Scale));
            }

            viewer.color(target.Color);
            viewer.viewPush(TargetTransform);
            viewer.rect(target.Bounds);
            viewer.viewPop();

        }

        public struct TargetType
        {
            public BoundingBox Bounds;
            public Matrix Pose;
            public char Color;
            public TargetType(BoundingBox box, Matrix pose, char color)
            {
                Bounds = box;
                Pose = pose;
                Color = color;
            }
        }
    }

    public class RadarDisplay
    {
        public Action<string> logdelegate = null;
        void Log(string msg) { logdelegate?.Invoke(msg); }

        IMyTextPanel display;
        Canvas canvas;
        Draw3 draw3;
        Scene scene;
        RadarModel radarmodel;
        long iter;

        Vector3UByte[] RelationshipColors = new Vector3UByte[5];

        Matrix scaletransform;

        public float scale { set { scaletransform = Matrix.CreateScale(1f / value); View = viewtransform * scaletransform; } }

        Matrix viewtransform = MatrixD.CreateLookAt(-Vector3D.UnitY, Vector3D.Zero, Vector3.UnitX);

        Matrix View;

        Vector2I getPanelSize(IMyTextPanel panel)
        {
            float fontSize = panel.GetValueFloat("FontSize") * 1.2f;

            float f_width = 21f;
            float f_height = 22f;

            int width = (int)Math.Floor(f_width / fontSize);
            int height = (int)Math.Floor(f_height / fontSize);

            return new Vector2I(width, height);
        }

        public void DrawDisplay(Matrix viewpoint, LidarTracker tracker)
        {
            canvas.clear(0,0,0);
            radarmodel.TargetTransform = Matrix.Invert(viewpoint) * View;
            updateRadarModelTargets(tracker);
            scene.draw(draw3);
            canvas.move(Vector2I.One * 5);
            canvas.setColor(Canvas.rgb((byte)((iter % 7) + 1), (byte)((iter % 5) + 3), (byte)((iter % 11) / 2 + 2)));
            canvas.circle((int)(iter % 37) / 6 + 1);
            canvas.FlushToDisplay(display);
            iter++;
        }

        void updateRadarModelTargets(LidarTracker tracker)
        {
            radarmodel.targets.Clear();
            foreach (TrackedEntity e in tracker.Entities) if (tracker.Now() - e.LastTime < 6000)
                {

                    RadarModel.TargetType t = new RadarModel.TargetType();
                    Vector3D pos_correction = e.PredictedPosition(tracker.Now()) - e.Position;
                    t.Bounds = new BoundingBox(e.Bound.Min + pos_correction, e.Bound.Max + pos_correction);
                    t.Pose = e.Orientation;
                    byte div = ((tracker.Now() - e.LastTime) > 3000) ? (byte)2 : (byte)1;
                    Vector3UByte c = RelationshipColors[(int)e.Relationship];
                    t.Color = Canvas.rgb((byte)(c.X / div), (byte)(c.Y / div), (byte)(c.Z / div));
                    radarmodel.targets.Add(t);
                }
        }

        public RadarDisplay(IMyTextPanel screen)
        {
            display = screen;
            canvas = new Canvas(getPanelSize(display));
            draw3 = new Draw3(canvas, Vector2I.Zero, new Vector2I(canvas.height, canvas.width));
            scene = new Scene();
            radarmodel = new RadarModel();

            draw3.view(Matrix.CreateLookAt(new Vector3(-5, 0, -2), Vector3.Zero, Vector3.UnitZ) *
                                      Matrix.CreateReflection(new Plane(Vector3.UnitX, 0.0f)));
            scene.models.Add(radarmodel);

            RelationshipColors[(int)MyRelationsBetweenPlayerAndBlock.Enemies] = new Vector3UByte(5, 0, 0);
            RelationshipColors[(int)MyRelationsBetweenPlayerAndBlock.FactionShare] = new Vector3UByte(0, 4, 0);
            RelationshipColors[(int)MyRelationsBetweenPlayerAndBlock.Neutral] = new Vector3UByte(4, 4, 4);
            RelationshipColors[(int)MyRelationsBetweenPlayerAndBlock.Owner] = new Vector3UByte(0, 3, 6);
            RelationshipColors[(int)MyRelationsBetweenPlayerAndBlock.NoOwnership] = new Vector3UByte(0, 5, 5);
        }

    }

    public class MyTuple<T1, T2> : IComparable<MyTuple<T1, T2>>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }

        public MyTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        private static readonly IEqualityComparer<T1> Item1EqComparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> Item2EqComparer = EqualityComparer<T2>.Default;

        private static readonly IComparer<T1> Item1Comparer = Comparer<T1>.Default;
        private static readonly IComparer<T2> Item2Comparer = Comparer<T2>.Default;

        public override int GetHashCode()
        {
            var hc = 0;
            if (!object.ReferenceEquals(Item1, null))
                hc = Item1EqComparer.GetHashCode(Item1);
            if (!object.ReferenceEquals(Item2, null))
                hc = (hc << 3) ^ Item2EqComparer.GetHashCode(Item2);
            return hc;
        }
        public override bool Equals(object obj)
        {
            var other = obj as MyTuple<T1, T2>;
            if (object.ReferenceEquals(other, null))
                return false;
            else
                return Item1Comparer.Compare(Item1, other.Item1) == 0 && Item2Comparer.Compare(Item2, other.Item2) == 0;
        }

        public override string ToString()
        {
            return String.Format("Tuple({0},{1})", Item1, Item2);
        }

        int IComparable<MyTuple<T1, T2>>.CompareTo(MyTuple<T1, T2> other)
        {
            int comp1 = Item1Comparer.Compare(this.Item1, other.Item1);
            if (comp1 != 0) { return comp1; }
            else
            {
                return Item2Comparer.Compare(this.Item2, other.Item2);
            }
        }
    }

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


public static class canvasExtension
{
    public static System.Drawing.Bitmap toBitMap(this Canvas c)
    {
        var bitmap = new System.Drawing.Bitmap(c.width, c.height);
        for (int i=0;i<c.width;i++)
            for (int j = 0; j < c.height; j++)
            {
                char cpixel = c.getPixel(i, j);

                //(char)(0xe100 + (r << 6) + (g << 3) + b)
                byte r = (byte)((cpixel- 0xe100) >> 6 & 7);
                byte g = (byte)((cpixel - 0xe100) >> 3 & 7);
                byte b = (byte) ((cpixel - 0xe100) & 7);

                System.Drawing.Color pixel = System.Drawing.Color.FromArgb(
                    r * 36 + r * 109 / 255,
                    g * 36 + g * 109 / 255, 
                    b * 36 + b * 109 / 255);
                bitmap.SetPixel(i,j,pixel);
            }

        return bitmap;
    }
}

[TestClass()]
public class test_canvas
{
    
    [TestMethod()]
    public void test_line()
    {
        Canvas c = new Canvas(new Vector2I(320, 320));
        c.clear(0,0,0);
        c.move(new Vector2I(100,100));
        c.setColor(0, 0, 7);
        c.line(new Vector2I(200, 140));
        c.move(new Vector2I(150, 150));
        c.setColor(6, 0, 0);
        c.circle(100);
        c.setColor(0, 6, 0);
        c.triangle(new Vector2I(170, 150), new Vector2I(150, 170));
        System.Drawing.Bitmap bmp = c.Bitmap;
    }

    [TestMethod()]
    public void test_TrianglePointGenerator()
    {
        var bmp = new System.Drawing.Bitmap(1000, 1000);
        System.Drawing.Graphics.FromImage(bmp).Clear(System.Drawing.Color.Black);

        var a = new Vector2I(7, 13);
        var b = new Vector2I(47, 50);
        var c = new Vector2I(97, 89);
        setPixel(bmp, System.Drawing.Color.Red, a);
        setPixel(bmp, System.Drawing.Color.Green, b);
        setPixel(bmp, System.Drawing.Color.Blue, c);
        var tri = new TrianglePointGenerator(a, b, c);

        do
        {
            setPixel(bmp, System.Drawing.Color.White, tri.Pt);
        } while (tri.Advance());
      }

    private static void setPixel( System.Drawing.Bitmap bmp, System.Drawing.Color c, Vector2I pt)
    {
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 10; j++)
                bmp.SetPixel(pt.X*10 + i, pt.Y*10 + j, c);
    }

    [TestMethod()]
    public void test_BresenhamLineGenerator()
    {
        var bmp = new System.Drawing.Bitmap(100, 100);
        System.Drawing.Graphics.FromImage(bmp).Clear(System.Drawing.Color.Black);

        var a = new Vector2I(3, 1);
        var b = new Vector2I(4, 7);
        setPixel(bmp, System.Drawing.Color.Red, a);
        setPixel(bmp, System.Drawing.Color.Green, b);
        var line = new BresenhamLineGenerator(a, b);

        do
        {
            setPixel(bmp, System.Drawing.Color.White, line.Pt);
        } while (line.Advance());
    }

    [TestMethod()]
    public void test_Draw3()
    {
        var canvas = new Canvas(new Vector2I(320, 320));
        var draw3 = new Draw3(canvas, Vector2I.Zero, Vector2I.One * 320);

        draw3.triangle()

    }
}

