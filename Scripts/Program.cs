using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using SpaceEngineersIngameScript.TargetTracking;
using SpaceEngineersIngameScript.Drawing.Radar;

namespace SpaceEngineersIngameScript.Scripts
{
    class LIDARTrackerProgram : MyGridProgram
    {
        #region "-------------INGAME PROGRAM--------------"

        const string RADAR_DISPLAY_NAME = "Radar LCD";
        const string SENSOR_NAME = "Sensor";
        const string SPOTTER_NAME = "Camera sp";
          
        LidarTracker lidarsystem;
        RadarDisplay radardisplay;
        IMySensorBlock sensor;
        IMyTerminalBlock viewpoint;
        IMyTerminalBlock spotter;

        IMyLargeTurretBase tur;

        int iter = 0;

        void Log(string message)
        {
            Echo(">" + message);
            var dServer = GridTerminalSystem.GetBlockWithName("DebugSrv") as IMyProgrammableBlock;
            if (dServer != null) { dServer.TryRun("L" + message); }
        }

        void TargetLog(string message)
        {
            Echo(">" + message);
            var dServer = GridTerminalSystem.GetBlockWithName("TargetSrv") as IMyProgrammableBlock;
            if (dServer != null) { dServer.TryRun("L" + message); }
        }

        public LIDARTrackerProgram()
        {
            Echo("Init!");
            lidarsystem = new LidarTracker(Me.CubeGrid);
            lidarsystem.logdelegate = Log;
            Echo("Init lidarsystem");
            List<IMyCameraBlock> sensors = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(sensors);
            lidarsystem.Init_Sensors(sensors);

            Echo("Init radardisplay");
            radardisplay = new RadarDisplay(GridTerminalSystem.GetBlockWithName(RADAR_DISPLAY_NAME) as IMyTextPanel);
            radardisplay.scale = 50;

            Echo("Init sensor");
            sensor = GridTerminalSystem.GetBlockWithName(SENSOR_NAME) as IMySensorBlock;

            Echo("Init spotter");
            spotter = GridTerminalSystem.GetBlockWithName(SPOTTER_NAME);

            Echo("Init cockpit");
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType<IMyCockpit>(cockpits);
            viewpoint = cockpits[0];

            tur = GridTerminalSystem.GetBlockWithName("turret") as IMyLargeTurretBase;
        }

        public void Main(string arg)
        {

            Echo("Main: " + iter.ToString() + " t=" + lidarsystem.Now());
            lidarsystem.UpdateTimeFromProgramTick(Runtime, iter);
            if (iter % 2 == 1)
            {
                Main_Tick();
            }
            else
            {
                lidarsystem.logdelegate = null;
                for (int i = 0; i < 100; i++)
                {
                    Vector3D target = spotter.WorldMatrix.Forward * 1500.0 + spotter.Position;
                    if (i != 0)
                    {

                        Vector3D u = spotter.WorldMatrix.Up;
                        Vector3D w = spotter.WorldMatrix.Left;
                        const double l = 5;

                        //search path is a spiral, where each ring is l apart, and each successive point is l apart.       
                        double theta = Math.Sqrt(Math.PI * (2 * (i - 1) + Math.PI)) - Math.PI;
                        double r = l * (theta / 2 / Math.PI + 1);

                        target += u * r * Math.Cos(theta) + w * r * Math.Sin(theta);
                    }
                    var rtn = lidarsystem.PingPosition(target);
                    if (!rtn.Item1 | rtn.Item2 != null)
                    {
                        //Log(t.ToString("0.0") + " " + (rtn.Item2==null?"null":rtn.Item2.ToString()));         
                        break;
                    }
                }
                lidarsystem.logdelegate = Log;
            }
            iter++;
        }

        void Main_Tick()
        {
            Echo("tick");
            lidarsystem.logdelegate = Log;
            lidarsystem.PingPlannedTargets();
            sensorupdate();
            radardisplay.DrawDisplay(viewpoint.WorldMatrix, lidarsystem);

            if (iter % 3 == 0)
            {
                lidarsystem.Entities.TrimEntities(lidarsystem.Now() - 12000);
                TargetLog("PingQueue: " + lidarsystem.pingqueue.Count + "    first in: " +
                    (lidarsystem.pingqueue.Count > 0 ?
                        ((lidarsystem.pingqueue.peekFirst() - lidarsystem.Now()) / 1000.0).ToString() :
                        "never"));
                TargetLog("Targets: " + lidarsystem.Entities.Count());
                foreach (var t in lidarsystem.Entities)
                {
                    TargetLog(t.ToString());
                }
            }
            Echo("EndTick");
        }

        void sensorupdate()
        {
            lidarsystem.UpdateWithExternalObservation(sensor.LastDetectedEntity);
        }

        #endregion
    }
}

