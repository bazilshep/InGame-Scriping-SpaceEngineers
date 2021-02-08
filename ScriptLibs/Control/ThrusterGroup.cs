using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll

namespace IngameScript.Control
{

    /// <summary>      
    /// Class for commanding a group of thrusters. Note: If any of the      
    /// Thrusters are removed/etc then the class instance should be thrown      
    /// away and a new one constructed.      
    /// </summary>      
    public class ThrusterGroup
    {
        List<IMyThrust> Thrusters;
        List<IMyThrust>[] DirThrusters;
        float[] max_thrust;
        public ThrusterGroup(List<IMyThrust> thrusters)
        {
            Thrusters = new List<IMyThrust>(thrusters);
            InitThrusters();
        }
        void InitThrusters()
        {
            DirThrusters = new List<IMyThrust>[6];
            max_thrust = new float[6];
            for (int i = 0; i < 6; i++)
                DirThrusters[i] = new List<IMyThrust>();
            foreach (var thr in Thrusters)
            {
                DirThrusters[(int)(thr.Orientation.Forward)].Add(thr);
                max_thrust[(int)(thr.Orientation.Forward)] += thr.MaxThrust;
            }
        }
        void SetThrusterOverride(IMyThrust thr, float val)
        {
            if (thr.IsWorking & thr.Enabled)
            {
                thr.SetValueFloat("Override", val);
            }
        }
        /// <summary>      
        /// Sets the 3D thrust vector of the group of thrusters.      
        /// </summary>      
        /// <param name="thrust"></param>      
        public void SetThrust(Vector3 thrust)
        {
            SetThrustAxis(Base6Directions.Axis.LeftRight, thrust.X);
            SetThrustAxis(Base6Directions.Axis.UpDown, -thrust.Y);
            SetThrustAxis(Base6Directions.Axis.ForwardBackward, thrust.Z);
        }
        void SetThrustAxis(Base6Directions.Axis ax, float thrust)
        {
            Base6Directions.Direction dirM = (Base6Directions.Direction)((byte)ax * 2);
            Base6Directions.Direction dirP = (Base6Directions.Direction)((byte)ax * 2 + 1);
            bool pos = thrust > 0.0f;
            SetThrustChannel(dirM, pos ? 0.0f : -thrust);
            SetThrustChannel(dirP, pos ? thrust : 0.0f);
        }
        void SetThrustChannel(Base6Directions.Direction dir, float thrust)
        {
            List<IMyThrust> thrusters = DirThrusters[(int)dir];
            foreach (var thr in thrusters)
                SetThrusterOverride(thr, thrust * thr.MaxThrust / MaxThrust(dir));
        }
        /// <summary>      
        /// Gets the maximum thrust in each direction of the CubeGrid.      
        /// </summary>      
        /// <param name="dir"></param>      
        /// <returns></returns>      
        public float MaxThrust(Base6Directions.Direction dir) { return max_thrust[(int)(dir)]; }
    }
    
}