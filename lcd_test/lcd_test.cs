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
using IngameScript.Drawing;

namespace IngameScript
{

    partial class lcd_test : MyGridProgram
    {

        IMyShipController cockpit;
        IMyTextSurface _drawingSurface;

        Frame3D scene = new Frame3D();
        Cube cube = new Cube();
        Cube cube2 = new Cube();

        bool oddcall = false;

        Vector3 input_position;
        Vector2 input_rotation;

        IMyBroadcastListener broadcast;

        string messagekey = "k3khdo3l";

        // Script constructor
        public lcd_test()
        {

            //var console = this.Me.GetSurface(0);
            //console.WriteText("Constructor Start", false);

            cockpit = (IMyShipController)this.GridTerminalSystem.GetBlockWithName("Control Stations 2");
            var surf_provider = (IMyTextSurfaceProvider)this.GridTerminalSystem.GetBlockWithName("Control Stations 2");

            _drawingSurface = surf_provider.GetSurface(0);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            scene.SetProjection(
                new RectangleF(
                    Vector2.Zero,
                    _drawingSurface.TextureSize)
                , 90f);

            Frame3D.PrepareTextSurfaceForSprites(_drawingSurface);

            cube.color = new Color(255, 30, 30);
            cube2.color = new Color(30, 30, 255);

            //console.WriteText("Constructor End", false);
        }

        // Main Entry Point
        public void Main(string argument, UpdateType updateType)
        {

            //var console = this.Me.GetSurface(0);
            //console.WriteText("Main",false);
            //console.WriteText(_t.ToString("F2"), true);

            var rot = cockpit.RotationIndicator;
            input_position += cockpit.MoveIndicator;
            input_rotation += cockpit.RotationIndicator;

            oddcall = !oddcall;

            scene.Clear();

            //var v = new Vector3((float)Math.Sin(_t), (float)Math.Cos(_t), (float)Math.Cos(_t * .1) + 5);
            var v = new Vector3(-input_rotation.Y * .005f - input_position.X * .05f, -input_rotation.X * .005f - input_position.Z * .05f, 5 + input_position.Y * .05f);

            cube.trf = Matrix.CreateLookAtInverse(new Vector3(3, 0, 10), v, Vector3.UnitY);
            cube.AddTo(scene, ref Matrix.Identity);

            cube2.trf = Matrix.CreateLookAtInverse(new Vector3(-3, 0, 8), v, Vector3.UnitY);
            cube2.AddTo(scene, ref Matrix.Identity);

            var markercolor = new Color(0, 100, 0);
            var sz = new Vector2(100f, 100f);
            
            scene.AddSprite(Sprites.Circle, ref scene.viewprojection, ref scene.view, ref sz, ref v, ref markercolor);

            var frame = _drawingSurface.DrawFrame();

            if (oddcall) frame.Add(new MySprite());

            scene.Draw(ref frame);

            frame.Dispose();

        }



    }
}