using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Drawing.Radar
{

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
    
}

