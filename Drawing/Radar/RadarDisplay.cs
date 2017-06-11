using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using System;
using VRage.Game; // VRage.Game.dll
using VRageMath; // VRage.Math.dll
using SpaceEngineersIngameScript.TargetTracking;

namespace SpaceEngineersIngameScript.Drawing.Radar
{

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
    
}

