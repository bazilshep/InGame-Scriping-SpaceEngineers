using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll

namespace SpaceEngineersIngameScript.Control
{

    /// <summary>      
    /// Class for controlling the position of a CubeGrid using      
    /// Thrusters. Note: the controller will position the Center      
    /// of Mass of the grid at the commanded position (as      
    /// calculated at class construction).      
    /// </summary>      
    public class PositionController
    {
        public Action<String> LogDelegate = null;
        private void Log(String msg) { LogDelegate?.Invoke(msg); }
        readonly IMyCubeGrid Grid;
        ThrusterGroup Thrusters;
        public readonly Matrix I;
        /// <summary>      
        /// PID Proportional gain. (in m/s^2 per m)      
        /// </summary>      
        public float Gain = 1.0f;
        /// <summary>      
        /// PID Derivative gain divided by PID proportional gain.      
        /// </summary>      
        public float DGain = .5f;
        /// <summary>      
        /// PID Integral gain divided by PID proportional gain.      
        /// </summary>      
        public float IGain = 0.1f;
        /// <summary>      
        /// Maximum Integral term (m/s^2)      
        /// </summary>      
        public float IMax = 1.0f;
        /// <summary>      
        /// Maximum following error (in meters) before the controller faults out (disables self).      
        /// Use a path planner to smoothly command a move between orientations (using ).      
        /// Don't just increase this or the controller will saturate the actuator and oscillate.      
        /// </summary>      
        public float ErrMax = 1f;
        /// <summary>      
        /// Allows enabling/disabling the control system.      
        /// </summary>      
        public bool Enabled
        {
            get { return _enabled; }
            set { if (!value) { Thrusters.SetThrust(Vector3.Zero); DCommandFilt = Vector3.Zero; } _enabled = value;}
        }
        bool _enabled = true;
        public float UpdateFrequency = 60.0f;
        public PositionController(List<IMyThrust> thrusters, IMyCubeGrid grid)
        {
            Thrusters = new ThrusterGroup(thrusters);
            Grid = grid;
            I = Inertia(grid);
        }
        public Vector3 CurrentPosition() { return Vector3D.Transform(I.Translation, Grid.WorldMatrix); }
        /// <summary>      
        /// Updates the control system using the next target position.      
        /// </summary>      
        /// <param name="target"></param>      
        /// <returns>Following error (in meters) (returns null if not Enabled)</returns>      
        public Vector3? Update(Vector3D target)
        {
            Vector3 err = target - CurrentPosition();
            Vector3 cmd = -Gain * err;
            if (Enabled & err.Length() > ErrMax) Enabled = false;
            if (float.IsNaN(cmd.X) | float.IsNaN(cmd.Y) | float.IsNaN(cmd.Z) |
                float.IsInfinity(cmd.X) | float.IsInfinity(cmd.Y) | float.IsInfinity(cmd.Z))
                Enabled = false;
            if (Enabled)
            {
                Command(cmd);
                return err;
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
            DCommandFilt += .2f * (DCommand - DCommandFilt);
            ICommand += Command * (1.0f / UpdateFrequency);
            Clamp(ref ICommand, IMax);
            Vector3 sumCmd = Command + DGain * DCommand + IGain * ICommand;
            Vector3 gCmd = Vector3.TransformNormal(sumCmd, MatrixD.Transpose(Grid.WorldMatrix.GetOrientation()));
            Thrusters.SetThrust(gCmd * I.M44);
            prevCommand = Command;

        }
        static void Clamp(ref Vector3 v, float max)
        { //Clamp a vector to maintain the direction, but impose a maximum magnitude       
            if (v.Length() > max) { v.Normalize(); v = v * max; }
        }

        /// <summary>      
        /// Function for computing the inertia of a CubeGrid. Returns the Moment      
        /// of Inertia (about the CoM) matrix, Center of Mass (offset from      
        /// cubegrid origin), and the total mass.      
        /// </summary>      
        /// <param name="grid"></param>      
        /// <returns></returns>      
        public static Matrix Inertia(IMyCubeGrid grid)
        {//computes a matrix containing the moment of inertia (about CoM), CoM, and TotalMass      
            Vector3I gridMin = grid.Min;
            Vector3I gridMax = grid.Max;

            Vector3 FirstMassMoment = Vector3.Zero;
            Matrix SecondMassMoment = Matrix.Zero;
            float TotalMass = 0.0f;
            float gridsize = grid.GridSize;

            for (var itr = new Vector3I_RangeIterator(ref gridMin, ref gridMax); itr.IsValid(); itr.MoveNext())
            {
                IMySlimBlock c = grid.GetCubeBlock(itr.Current);
                if (c != null && c.Position == itr.Current)
                { //prevent double counting blocks which span multiple positions      
                    IMyCubeBlock b = c.FatBlock;
                    Vector3 sz = Vector3.One * gridsize;
                    if (b != null) sz = (b.Max - b.Min) * gridsize;
                    float mass = c.Mass;
                    TotalMass += mass;
                    Vector3 r = gridsize * c.Position;
                    float rr;
                    Vector3.Dot(ref r, ref r, out rr);

                    FirstMassMoment += r * mass;

                    //moment of inertia from point mass      
                    SecondMassMoment.M11 += (rr - r.X * r.X) * mass;
                    SecondMassMoment.M22 += (rr - r.Y * r.Y) * mass;
                    SecondMassMoment.M33 += (rr - r.Z * r.Z) * mass;
                    SecondMassMoment.M12 += -(r.X * r.Y) * mass;
                    SecondMassMoment.M13 += -(r.X * r.Z) * mass;
                    SecondMassMoment.M23 += -(r.Y * r.Z) * mass;

                    //momemnt of inertia from solid cube      
                    float cubefactor = mass / 12.0f;
                    SecondMassMoment.M11 += cubefactor * (sz.Y * sz.Y + sz.Z * sz.Z);
                    SecondMassMoment.M22 += cubefactor * (sz.X * sz.X + sz.Z * sz.Z);
                    SecondMassMoment.M33 += cubefactor * (sz.X * sz.X + sz.Y * sz.Y);
                }
            }

            Vector3 CoM = FirstMassMoment / TotalMass;
            float CoMSq = CoM.Dot(CoM);
            MatrixD I = SecondMassMoment;
            //translate second moment of intertia tensor to be about the CoM, rather than the cubegrid origin      
            I.M11 += (CoMSq - CoM.X * CoM.X) * TotalMass;
            I.M22 += (CoMSq - CoM.Y * CoM.Y) * TotalMass;
            I.M33 += (CoMSq - CoM.Z * CoM.Z) * TotalMass;
            I.M12 = -(CoM.X * CoM.Y) * TotalMass;
            I.M13 = -(CoM.X * CoM.Z) * TotalMass;
            I.M23 = -(CoM.Y * CoM.Z) * TotalMass;
            I.M21 = I.M12;
            I.M31 = I.M13;
            I.M32 = I.M23;
            I.Translation = CoM; //since we have extra space in the matrix, stick the CoM in the otherwise unused 4th column      
            I.M44 = TotalMass; //since we have extra space, stick the total mass in the 44 element      
            return I;
        }
    }

}