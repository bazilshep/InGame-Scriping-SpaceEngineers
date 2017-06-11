using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using SpaceEngineersIngameScript.Drawing;

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

        //draw3.triangle();

    }
}

