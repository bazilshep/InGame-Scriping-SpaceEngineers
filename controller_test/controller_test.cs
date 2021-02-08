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
using IngameScript.Control;
using IngameScript.Drawing;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        string messagekey = "k3khdo3l";
        string channelkey = "fkjdoexhefk";
        string loggerkey = "dkdjx,eo3d8";

        MyWaypointInfo returnwaypoint = new MyWaypointInfo("return waypoint", 11.02, 0.04, 37.42);

        IMyCubeBlock refblock;
        IMyShipController cockpit;
        IMyTextSurface _drawingSurface;
        RectangleF _viewport;

        Frame3D scene = new Frame3D();

        bool oddcall = false;

        Matrix commanded_position = Matrix.CreateTranslation(10, 0, 20);
        List<Matrix> commanded_waypoints = new List<Matrix>();
        Cube commanded_position_model = new Cube();

        Cube actual_position_model = new Cube();        

        IMyBroadcastListener broadcast;

        Logger remotelogger;
        IMyBroadcastListener remotelogger_reciever;

        public Program()
        {
            this.Me.GetSurface(0).WriteText("", false);
            var logger = new Logger(this.Me.GetSurface(0), 19);
            Echo = (str) => { logger.Log(str); IGC.SendBroadcastMessage(loggerkey, str); };

            Echo("+Program\n");

            remotelogger = new Logger(
                ((IMyTextSurfaceProvider)this.GridTerminalSystem.GetBlockWithName("remotelog")).GetSurface(0),
                20);
            remotelogger_reciever = IGC.RegisterBroadcastListener(loggerkey);
            remotelogger_reciever.SetMessageCallback("remotelogger");

            broadcast = this.IGC.RegisterBroadcastListener(channelkey);
            broadcast.SetMessageCallback(messagekey);

            refblock = this.GridTerminalSystem.GetBlockWithName("Control Stations 2");
            cockpit = (IMyShipController)this.GridTerminalSystem.GetBlockWithName("Control Stations 2");
            var surf_provider = (IMyTextSurfaceProvider)this.GridTerminalSystem.GetBlockWithName("Control Stations 2");

            _drawingSurface = surf_provider.GetSurface(0);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            _viewport = new RectangleF(
                Vector2.Zero,
                _drawingSurface.TextureSize
            );

            scene.view = Matrix.CreateRotationY((float)Math.PI) *
                Matrix.CreateScale(.1f) *
                Matrix.CreateTranslation(0, 0, 1);
            scene.projection =
                Matrix.CreatePerspectiveFieldOfView(90f * (float)Math.PI / 180f, _viewport.Height / _viewport.Width, .01f, 100f) *
                Matrix.CreateScale(.5f * _viewport.Width, .5f * _viewport.Height, 1) *
                Matrix.CreateTranslation(.5f * _viewport.Width, .5f * _viewport.Height, 0);
            scene.viewprojection = scene.view * scene.projection;

            PrepareTextSurfaceForSprites(_drawingSurface);

            commanded_position_model.color = new Color(200, 30, 30);
            commanded_position_model.trf = Matrix.CreateTranslation(0, 5, 10);
            actual_position_model.color = new Color(30, 200, 30);

            Echo("~Program\n");
        }

        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface)
        {
            textSurface.ContentType = ContentType.SCRIPT;
            textSurface.Script = "";
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if (updateSource != UpdateType.Update1 & updateSource != UpdateType.IGC)
                {
                    Echo("arg:[" + argument + "]\n");
                }

                if (argument == "send")
                {
                    Echo("send");

                    var lst = System.Collections.Immutable.ImmutableArray<MatrixD>.Empty;
                    foreach (var c in commanded_waypoints) { lst = lst.Add(c * refblock.WorldMatrix); }
                    lst = lst.Add(commanded_position_model.trf * refblock.WorldMatrix);
                    this.IGC.SendBroadcastMessage(channelkey, lst);
                    commanded_waypoints.Clear();
                    Echo("~send\n");
                }
                if (argument == "return")
                {
                    Echo("return");
                    this.IGC.SendBroadcastMessage(channelkey, "return");
                    Echo("~return\n");
                }
                if (argument == "load")
                {
                    commanded_position = actual_position_model.trf;
                }
                if (argument == "save")
                {
                    commanded_waypoints.Add(commanded_position);
                }
                if (argument == "pop")
                {
                    commanded_waypoints.RemoveAt(commanded_waypoints.Count - 1);
                }

                if (argument == messagekey && updateSource== UpdateType.IGC)
                {
                    RecieveMessage();
                }

                while (remotelogger_reciever.HasPendingMessage)
                {
                    remotelogger.Log(remotelogger_reciever.AcceptMessage().As<string>());
                }

                update();

            } catch (Exception ex)
            {
                Echo(ex.ToString());
            }
        }

        byte t = 0;
        void update()
        {
            var roll = cockpit.RollIndicator * .01f;
            var rot = cockpit.RotationIndicator * .001f;
            var input_position = cockpit.MoveIndicator * .01f;

            commanded_position = Matrix.CreateRotationZ(-roll) *
                Matrix.CreateRotationX(-rot.X) *
                Matrix.CreateRotationY(-rot.Y) *
                Matrix.CreateTranslation(input_position) *
                commanded_position;

            //oddcall = !oddcall;

            //t++;
            //if ((t & 7) != 0) return;

            scene.Clear();

            commanded_position_model.trf = commanded_position;
            commanded_position_model.AddTo(scene,ref  Matrix.Identity);
            actual_position_model.AddTo(scene,ref Matrix.Identity);

            var markercolor = new Color(30, 30, 90);
            var sz = new Vector2(10f, 10f);

            foreach (var c in commanded_waypoints)
            {
                var v = c.Translation;
                scene.AddSprite(Sprites.Circle, ref scene.viewprojection, ref scene.view, ref sz, ref v, ref markercolor);
            }

            var frame = _drawingSurface.DrawFrame();

            //if (oddcall) frame.Add(new MySprite());

            scene.Draw(ref frame);

            frame.Dispose();
        }

        void RecieveMessage()
        {

            while (broadcast.HasPendingMessage)
            {

                var msg = broadcast.AcceptMessage();

                MatrixD mat;
                if (MyUtil.TryCast(msg.Data, out mat))
                {
                    actual_position_model.trf = mat * MatrixD.Invert(refblock.WorldMatrix);
                }

            }

        }

    }
}

