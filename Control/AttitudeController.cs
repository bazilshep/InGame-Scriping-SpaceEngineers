using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll

namespace SpaceEngineersIngameScript.Control
{
    /// <summary>       
    /// Class for controlling the attitude of a CubeGrid using gyroscopes       
    /// </summary>       
    public class AttitudeController
    {
        readonly IMyCubeGrid Grid;
        GyroGroup Gyros;
        /// <summary>       
        /// PID proportional gain (rad/s per rad)       
        /// </summary>       
        public float Gain = 1.0f;
        /// <summary>       
        /// PID derivative gain divided by PID Proportional gain       
        /// </summary>       
        public float DGain = 0.1f;
        /// <summary>       
        /// PID Integral gain divided by PID Proportional gain       
        /// </summary>       
        public float IGain = 0.1f;
        /// <summary>       
        /// Maximum Integral term (max commanded rotational velocity vector length)       
        /// </summary>       
        public float IMax = 1.0f;
        /// <summary>       
        /// Maximum following error (in radians) before the controller faults out (disables self).       
        /// Use a path planner to smoothly command a move between orientations (using SLERP).       
        /// Don't just increase this or the controller will saturate the actuator and oscillate.       
        /// </summary>       
        public float ErrMax = .1f;
        /// <summary>       
        /// Rate at which the Update function is called. This rate should be constant while Enabled!       
        /// </summary>       
        public float UpdateFrequency = 60.0f;
        /// <summary>       
        /// Allows enabling/disabling the control system.       
        /// </summary>       
        public bool Enabled
        {
            get { return _enabled; }
            set { if (value) Enable(); else { Disable();  DCommandFilt = Vector3.Zero; } _enabled = value; }
        }
        bool _enabled = false;
        /// <summary>       
        /// Creates an an AttitudeController using a list of gyroscopes, and their cubegrid.       
        /// All the gyroscopes must be on the same cubegrid!       
        /// </summary>       
        /// <param name="gyros"></param>       
        /// <param name="grid"></param>       
        public AttitudeController(List<IMyGyro> gyros, IMyCubeGrid grid)
        {
            Gyros = new GyroGroup(gyros);
            Grid = grid;
        }
        /// <summary>       
        /// Updates the control system using the next target orientation. This is useful for        
        /// </summary>       
        /// <param name="target">WorldMatrix describing the target orientation of the CubeGrid in space.</param>       
        /// <returns>Angle between the target orientation and the current orientation (in radians). Returns null if not Enabled</returns>       
        public float? Update(MatrixD target)
        {
            MatrixD orientation = Grid.WorldMatrix.GetOrientation();
            MatrixD err = Matrix.Transpose(MatrixD.Transpose(orientation) * target);
            Quaternion errq = Quaternion.CreateFromRotationMatrix(err);

            return UpdateErr(errq);
        }
        public float? Update(Quaternion target)
        {
            return Update(MatrixD.CreateFromQuaternion(target));
        }
        /// <summary>       
        /// Updates the control system using the next target orientation. This       
        /// overload does not fully constrain the orientation, and only aligns       
        /// an axis on the CubeGrid to an axis in the World. This is useful for       
        /// when you care about pointing something in a certian direction       
        /// (e.g. pointing a gun at a target, or pointing a thruster against gravity)       
        /// </summary>       
        /// <param name="axis"></param>       
        /// <param name="target"></param>       
        /// <returns>Angle between the target orientation and the current orientation (in radians). Returns null if not Enabled</returns>       
        public float? Update(Vector3 axis, Vector3D target)
        {//axis is in Grid coordinates, target is in World coordinates         
            Vector3 offset = target - Grid.GetPosition();
            Quaternion err = Quaternion.CreateFromTwoVectors(target, Vector3.TransformNormal(axis, Grid.WorldMatrix));
            return UpdateErr(err);
        }
        float? UpdateErr(Quaternion err)
        {
            Vector3 axis;
            float angle;
            err.GetAxisAngle(out axis, out angle);
            Vector3 axisGrid = Vector3.TransformNormal(axis, MatrixD.Transpose(Grid.WorldMatrix.GetOrientation()));
            Vector3 cmd = -angle * Gain * axisGrid;
            if (float.IsNaN(angle) | float.IsInfinity(angle) |
                float.IsNaN(axis.X) | float.IsInfinity(axis.X) |
                float.IsNaN(axis.Y) | float.IsInfinity(axis.Y) |
                float.IsNaN(axis.Z) | float.IsInfinity(axis.Z))
                Enabled = false;
            if (Enabled & angle > ErrMax) Enabled = false;
            if (Enabled)
            {
                Command(cmd);
                return angle;
            }
            else return null;
        }
        Vector3? prevCommand = Vector3.Zero;
        Vector3 ICommand = Vector3.Zero;
        Vector3 DCommandFilt = Vector3.Zero;
        void Command(Vector3 Command)
        {
            Vector3 DCommand = Vector3.Zero;
            if (prevCommand.HasValue) DCommand = (Command - prevCommand.Value) * UpdateFrequency;
            DCommandFilt += .1f * (DCommand - DCommandFilt);
            ICommand += Command * (1.0f / UpdateFrequency);
            Clamp(ref ICommand, IMax);
            Gyros.SetAngularVelocity(Command + DGain * DCommand + IGain * ICommand);
            prevCommand = Command;
        }
        static void Clamp(ref Vector3 v, float max)
        { //Clamp a vector to maintain the direction, but impose a maximum magnitude        
            if (v.Length() > max) { v.Normalize(); v = v * max; }
        }
        void Enable() { Gyros.Enable(); ICommand = Vector3.Zero; prevCommand = null; }
        void Disable() { Gyros.Disable(); ICommand = Vector3.Zero; prevCommand = null; }
        /// <summary>       
        /// Sets the PID gains according to the Ziegler-Nichols method.       
        /// </summary>       
        /// <param name="G"></param>       
        /// <param name="T"></param>       
        public void SetGainZieglerNichols(float G, float T)
        {
            Gain = .6f * G;
            DGain = 3.0f / 40.0f * T;
            IGain = 1.2f / T;
            IMax = 1.0f / IGain;
        }
    }
    
}