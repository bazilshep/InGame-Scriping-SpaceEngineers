using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using VRage.Game; // VRage.Game.dll
using System.Text;
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using Sandbox.Game.EntityComponents; // Sandbox.Game.dll
using VRage.Game.Components; // VRage.Game.dll
using VRage.Collections; // VRage.Library.dll
using VRage.Game.ObjectBuilders.Definitions; // VRage.Game.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll
using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace SpaceEngineersIngameScript.Scripts.Spotter
{
    public class Program : MyGridProgram
    {
        public void EchoF(string format, params object[] args)
        {
            Echo(string.Format(format, args));
        }

        public void Main(string argument)
        {

            IMyLargeTurretBase spotter = GridTerminalSystem.GetBlockWithName("spotter 1") as IMyLargeTurretBase;
            EchoF("Azimuth: {0}", spotter.Azimuth);
            EchoF("Elevation: {0}", spotter.Elevation);
            EchoF("Range: {0}", spotter.Range);
            double range = spotter.Range;
            double.TryParse(argument, out range);
            double ele = spotter.Elevation;
            double az = spotter.Azimuth;
            Vector3D up = spotter.WorldMatrix.Up;
            Vector3D fwd = spotter.WorldMatrix.Forward;
            Vector3D lft = spotter.WorldMatrix.Left;
            Vector3D dir = Math.Cos(ele) * (Math.Cos(az) * fwd + Math.Sin(az) * lft) + Math.Sin(ele) * up;
            EchoF("Dir: {0}", dir.ToString("0.000"));
            Vector3D pos = spotter.WorldMatrix.Translation + dir * range;
            EchoF("Pos: {0}", dir.ToString("0.000"));
            string waypoint = string.Format("GPS:target:{0}:{1}:{2}:", pos.X, pos.Y, pos.Z);
            Echo(waypoint);
            spotter.CustomData += waypoint + "\n";
        }

    }
}
