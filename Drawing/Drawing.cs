using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing
{

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

