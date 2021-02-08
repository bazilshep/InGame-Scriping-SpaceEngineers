using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript.Drawing
{

    public class Frame3D
    {

        float diffuse = .3f;
        float directional = .8f;

        int sprites_count = 0;
        const int SPRITES_MAX=1024;
        MySprite[] oldframe = new MySprite[SPRITES_MAX*3];
        sprite_with_depth[] sprites = new sprite_with_depth[SPRITES_MAX];
        float[] d = new float[SPRITES_MAX];
        int[] idx = new int[SPRITES_MAX];

        public Matrix view = Matrix.Identity;

        public Matrix projection;

        public Matrix viewprojection;

        public Vector3 LightDirection = new Vector3(0, -.1, -1);

        MySprite default_sprite = new MySprite(SpriteType.TEXTURE, Sprites.RightTriangle, new Vector2(0f, 0f), new Vector2(0f, 0f), new Color(0, 0, 0));

        public Frame3D()
        {
            for (var i = 0; i < SPRITES_MAX; i++)
            {
                sprites[i].s = default_sprite;
                oldframe[i * 3] = default_sprite;
                oldframe[i * 3].Data = Sprites.RightTriangle;
                oldframe[i * 3 + 1] = default_sprite;
                oldframe[i * 3 + 1].Data = Sprites.Circle;
                oldframe[i * 3 + 2] = default_sprite;
                oldframe[i * 3 + 2].Data = Sprites.Circle;
            }
            LightDirection.Normalize();
        }

        private static void triangle(ref MySprite s1, ref MySprite s2, ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {

            var lab = (b - a).Length();
            var lbc = (c - b).Length();
            var lac = (c - a).Length();

            MyUtil.SortSymmetricPairs(ref lab, ref lbc, ref lac, ref a, ref b, ref c);

            var ab = (b - a);
            var bc = (c - b);
            var ac = (c - a);

            var l_hat = ab / lab;
            var h_hat = new Vector2(ab.Y, -ab.X) / lab;
            var h = Vector2.Dot(h_hat, ac);
            var l1 = (float)Math.Sqrt(lac * lac - h * h);
            var l2 = lab - l1;

            var p = a + l1 * l_hat;
            var p1 = p - .5f * l1 * l_hat + .5f * h * h_hat;
            var p2 = p + .5f * l2 * l_hat + .5f * h * h_hat;

            var r = (float)Math.Atan2(ab.Y, ab.X);

            s1.Position = p1;
            s1.Size = new Vector2(-1.01f * l1, h);
            s1.RotationOrScale = r;
            s2.Position = p2;
            s2.Size = new Vector2(1.01f * l2, h);
            s2.RotationOrScale = r;
        }

        private bool triangle_to_sprite(ref Triangle t, ref Matrix modelviewprojection, ref sprite_with_depth s1, ref sprite_with_depth s2, ref Vector3 l)
        {

            Vector3 v1, v2, v3;
            Vector3.Transform(ref t.v1, ref modelviewprojection, out v1);
            Vector3.Transform(ref t.v2, ref modelviewprojection, out v2);
            Vector3.Transform(ref t.v3, ref modelviewprojection, out v3);

            var d = Math.Min(v1.Z, Math.Min(v2.Z, v3.Z));

            //if (d < 0) return false;

            var ab = v2 - v1;
            var ac = v3 - v1;
            //if ((ab.X * ac.Y - ab.Y * ac.X) > 0) return false;

            s1.d = d;
            s2.d = d;

            Vector2 v1_2, v2_2, v3_2;
            v1_2.X = v1.X;
            v2_2.X = v2.X;
            v3_2.X = v3.X;
            v1_2.Y = v1.Y;
            v2_2.Y = v2.Y;
            v3_2.Y = v3.Y;

            triangle(ref s1.s, ref s2.s, ref v1_2, ref v2_2, ref v3_2);

            var c = t.c.Shade(Light(t.n, l));
            s1.s.Color = c;
            s2.s.Color = c;
            s1.s.Data = Sprites.RightTriangle;
            s2.s.Data = Sprites.RightTriangle;
            s1.s.Type = SpriteType.TEXTURE;
            s2.s.Type = SpriteType.TEXTURE;

            return true;
        }

        public void AddTriangle( ref Triangle t, ref Matrix modelviewprojection, ref Vector3 l)
        {
            var added = triangle_to_sprite(ref t, ref modelviewprojection, ref sprites[sprites_count], ref sprites[sprites_count+1], ref l);
            if (added) sprites_count += 2;
        }

        public void AddSprite(string sprite, ref Matrix ModelViewProjection, ref Matrix ModelView, ref Vector2 sz, ref Vector3 p, ref Color c)
        {

            var p2 = Vector3.Transform(p, ModelView);

            if (p2.Z < 0) return;

            var pp = Vector3.Transform(p, ModelViewProjection);

            sprites[sprites_count].s.Data = sprite;
            sprites[sprites_count].s.Color = c;
            sprites[sprites_count].s.Size = sz * (1f / p2.Z);
            sprites[sprites_count].s.Position = new Vector2(pp.X, pp.Y);
            sprites[sprites_count].s.RotationOrScale = 0;
            sprites[sprites_count].d = pp.Z;
            sprites_count += 1;
        }

        private float Light(Vector3 n, Vector3 l)
        {
            //return 1f;
            float dir;
            Vector3.Dot(ref n, ref l, out dir);
            return Math.Min(diffuse + directional * Math.Max(dir, 0f), 1f);
        }

        byte t=0;
        public void Draw(ref MySpriteDrawFrame frame)
        {
            for (var i=0;i< sprites_count; i++)
            {
                idx[i] = i;
                d[i] = sprites[i].d;
            }
            
            Array.Sort(d, idx);
            for (var i = 0; i < sprites_count; i++)
            {
                frame.Add(sprites[idx[i]].s);                
            }
            //for (var i = 0; i < 10; i++)
            //{
            //    frame.Add(default_sprite);
            //}
        }
        public void Clear()
        {
            sprites_count = 0;
        }

    }

    public struct sprite_with_depth
    {
        public float d;
        public MySprite s;
    }

    public struct Triangle
    {
        public Vector3 v1,v2,v3;
        public Vector3 n;
        public Color c;
    }

}