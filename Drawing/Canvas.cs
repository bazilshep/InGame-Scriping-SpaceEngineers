using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System;
using System.Text;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

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
    
    }
