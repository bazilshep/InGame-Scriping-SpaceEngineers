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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        Autopilot AP;

        IMyBroadcastListener broadcast;

        IMyRemoteControl autopilot;
        
        string messagekey = "k3khdo3l";
        string channelkey = "fkjdoexhefk";
        string loggerkey = "dkdjx,eo3d8";

        public double MaxMidVelocity = 5;
        public double MaxAcceleration = 2;
        public double MaxMidAngularVelocity = 2;
        public double MaxAngularAcceleration = 1;

        public Program()
        {            

            this.Me.GetSurface(0).WriteText("", false);
            var logger = new Logger(this.Me.GetSurface(0),15);
            this.Echo = (str) => { logger.Log(str + "\n"); IGC.SendBroadcastMessage(loggerkey, str + "\n"); };

            Echo("Init!");

            broadcast = this.IGC.RegisterBroadcastListener(channelkey);
            broadcast.SetMessageCallback(messagekey);

            List<IMyGyro> gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            Echo("Gyros:" + gyros.Count.ToString());

            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            Echo("Thrusters:" + thrusters.Count.ToString());            

            AP = new Autopilot(thrusters, gyros, Me.CubeGrid);

            AP.TraceDelegate = (str) => this.Me.GetSurface(1).WriteText(str + "\n", true);
            AP.LogDelegate = Echo;

            AP.AC.SetGainZieglerNichols(240.0f, 1f);

            AP.PC.Gain = 10f;
            AP.Enabled = true;
            Echo("~Init!");

            autopilot = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName("Remote Control");

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        long i = 0;
        public void Main(string argument, UpdateType updateSource)
        {
            var dt = this.Runtime.TimeSinceLastRun.TotalSeconds;
            if (dt > .5 && AP.Enabled)
            {
                Echo("dt overrun " + dt.ToString("F2"));
                AP.Enabled = false;
            }
            if (argument == messagekey && updateSource==UpdateType.IGC)
                try
                {
                    HandleMessage();
                    Echo("~HandleMessage");
                }
                catch (Exception ex)
                {
                    Echo(ex.ToString());
                }

            this.Me.GetSurface(1).WriteText("", false);
            try
            {
                AP.Update(dt);
            }
            catch (Exception ex)
            {
                Echo(ex.ToString());
            }
            if (Me.CubeGrid.WorldMatrix.Translation.Length() > 200)
            {
                AP.Enabled = false;
                Echo("safety activated!\n");
            }

            IGC.SendBroadcastMessage(channelkey, Me.CubeGrid.WorldMatrix);
                
            i++;
        }

        void HandleMessage()
        {
            Echo("HandleMessage");
            if (!broadcast.HasPendingMessage) return;
            MyIGCMessage msg;
            do { 
                msg = broadcast.AcceptMessage();
            } while (broadcast.HasPendingMessage);
            Echo(msg.Data.GetType().ToString());

            System.Collections.Immutable.ImmutableArray<MatrixD> waypoints;
            if (MyUtil.TryCast(msg.Data, out waypoints))
            {
                var old = Me.CubeGrid.WorldMatrix;

                List<MatrixD> wp_list = new List<MatrixD>();
                wp_list.Add(old);
                wp_list.AddRange(waypoints);

                var path = new WaypointsPath(wp_list);
                path.MaxAcceleration = MaxAcceleration;
                path.MaxAngularAcceleration = MaxAngularAcceleration;
                path.MaxMidAngularVelocity = MaxMidAngularVelocity;
                path.MaxMidVelocity = MaxMidVelocity;

                AP.PathPlanner = path.GeneratePathSegment;
                AP.Enabled = true;
                autopilot.SetAutoPilotEnabled(false);
                Echo("Matrix waypoint");
                Echo(AP.PathPlanner.ToString());
            }

            System.Collections.Immutable.ImmutableArray<Vector3D> waypoints_v;
            if (MyUtil.TryCast(msg.Data,out waypoints_v))
            {
                var old = Me.CubeGrid.WorldMatrix;

                List<MatrixD> wp = new List<MatrixD>();
                wp.Add(old);
                var old_p = old.Translation;
                foreach(var pt in waypoints_v)
                {
                    var tgt = MatrixD.CreateLookAt(old_p, pt, old.Up);
                    tgt.Translation = pt;
                    wp.Add(tgt);
                    old_p = pt;
                }
                
                AP.PathPlanner = new WaypointsPath(wp).GeneratePathSegment;
                AP.Enabled = true;
                autopilot.SetAutoPilotEnabled(false);
                Echo("Vector waypoint");
                Echo(AP.PathPlanner.ToString());
            }

            //System.Collections.Immutable.ImmutableArray<MyWaypointInfo> waypoints_a;
            //if (MyUtil.TryCast(msg.Data, out waypoints_a))
            //{

            //    autopilot.ClearWaypoints();
            //    foreach (var w in waypoints_a)
            //    {
            //        autopilot.AddWaypoint(w);
            //    }
            //    autopilot.FlightMode = FlightMode.OneWay;
            //    autopilot.WaitForFreeWay = true;
            //    autopilot.SetAutoPilotEnabled(true);
            //    AP.Enabled = false;

            //    Echo("autopilot waypoint");
            //}

            var str = msg.Data as string;
            if (str == "return")
            {
                autopilot.SetAutoPilotEnabled(true);
                AP.Enabled = false;

                Echo("autopilot waypoint");
            }


        }


    }
}

