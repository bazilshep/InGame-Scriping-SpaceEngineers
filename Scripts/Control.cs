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

using SpaceEngineersIngameScript.Scripts;

namespace SpaceEngineersIngameScript.Scripts
{

    public class Autopilot
    {
        public Action<string> LogDelegate;
        private void Log(string msg) { LogDelegate?.Invoke(msg); }

        private IMyCubeGrid Grid;

        public PositionController PC;
        public AttitudeController AC;

        private PositionPlanner PP;

        //private double UpdateFrequency = 60.0; 
        private double UpdatePeriod = 1 / 60.0;

        private List<PositionPlanner.Waypoint> WaypointList;

        public Autopilot(List<IMyThrust> thrusters, List<IMyGyro> gyros, IMyCubeGrid grid)
        {
            PC = new PositionController(thrusters, grid);
            AC = new AttitudeController(gyros, grid);
            PP = new PositionPlanner(new PositionPlanner.Waypoint(PC.CurrentPosition(), Vector3D.Zero));
            Grid = grid;
            WaypointList = new List<PositionPlanner.Waypoint>();
            PP.waypoints = new StackAsEnumerator<PositionPlanner.Waypoint>(WaypointList);
        }
        public void Update()
        {
            Vector3D cmdP = PP.NextPosition(UpdatePeriod);
            Log("CmdP: " + cmdP.ToString("0.00"));
            Vector3? errP = PC.Update(cmdP);
            Log("ErrP: " + (errP.HasValue ? errP.Value.ToString("0.00") : "null"));
            float? errA = AC.Update(Grid.WorldMatrix);
            Log("ErrA: " + (errA.HasValue ? errA.Value.ToString("0.00") : "null"));
        }
        public void AddPosition(Vector3D pos)
        {
            WaypointList.Add(new PositionPlanner.Waypoint(pos, Vector3D.Zero));
        }
    }

    public class StackAsEnumerator<T> : IEnumerator<T>
    {
        public StackAsEnumerator(IList<T> wrapped)
        { Wrapped = wrapped; }
        private bool initialized = false;
        private IList<T> Wrapped;

        public T Current
        { get { return Wrapped[0]; } }

        object IEnumerator.Current
        { get { return Current; } }

        public void Dispose() { }

        public bool MoveNext()
        {
            if (Wrapped.Count > 0)
            {
                if (initialized)
                    Wrapped.RemoveAt(0);
                initialized = true;
                return Wrapped.Count > 0;
            }
            initialized = false;
            return false;
        }

        public void Reset()
        { }
    }

    public class PositionPlanner
    {

        public class Waypoint
        {
            public readonly Vector3D Position;
            public readonly Vector3D Velocity;
            public Waypoint(Vector3D position, Vector3D velocity)
            {
                Position = position;
                Velocity = velocity;
            }
            public Vector3D Extrapolate(double dt) { return dt * Velocity + Position; }
            public Waypoint ExtrapolatedWaypoint(double dt) { return new Waypoint(Extrapolate(dt), Velocity); }
        }
        public class PathSegment
        {
            public readonly Waypoint Initial;
            public readonly Waypoint Final;
            public readonly double dt;
            public readonly Vector3D dV;
            public readonly Vector3D dP;
            public PathSegment(Waypoint initial, Waypoint final, double MaxMidVelocity, double MidAcceleration)
            {
                Initial = initial;
                Final = final;
                dP = Final.Position - Initial.Position;
                dV = Final.Velocity - Initial.Velocity;
                double AccelDist = 0.5 * dP.Length();
                double v = (Final.Velocity + Initial.Velocity).Dot(dP) / dP.Length();
                double PeakMidVelocity = Math.Min(MaxMidVelocity, Math.Sqrt(Math.Abs(AccelDist * MidAcceleration) + v * v));
                double AvgVelocity = .5 * (v + PeakMidVelocity);
                dt = (AccelDist > 0) ? 2 * AccelDist / AvgVelocity : .001;
            }
            public Vector3D Position(double t)
            {
                double interp = Interp(t);
                return interp * Final.Extrapolate(t - dt) + (1.0 - interp) * Initial.Extrapolate(t);
            }
            public Vector3D Velocity(double t)
            {
                double interp = Interp(t);
                return Initial.Velocity * (1.0 - interp) + interp * Final.Velocity +
                         dInterp(t) * (Final.Extrapolate(t - dt) - Initial.Extrapolate(t));
            }
            public Vector3D Acceleration(double t)
            {
                return 2.0 * Interp(t) * dV + ddInterp(t) * (Final.Extrapolate(t - dt) - Initial.Extrapolate(t));
            }
            public Waypoint InterpolateWaypoint(double t)
            { return new Waypoint(Position(t), Velocity(t)); }
            private double Interp(double t) { return (1.0 - Math.Cos(Math.PI * t / dt)) * .5; }
            private double dInterp(double t) { double c = Math.PI / dt; return 0.5 * c * Math.Sin(c * t); }
            private double ddInterp(double t) { double c = Math.PI / dt; return 0.5 * c * c * Math.Cos(c * t); }
        }

        public double PlannerAcceleration = 10;
        public double PlannerVelocity = 90;

        public IEnumerator<Waypoint> waypoints;
        private PathSegment CurrentSegment;
        private Waypoint LastWaypoint;
        double CurrentSegmentTime;

        public PositionPlanner(Waypoint init)
        { Reset(init); }

        public Vector3D NextPosition(double dt)
        {
            CurrentSegmentTime += dt;
            while (true)
                if (CurrentSegment != null)
                {
                    if (CurrentSegmentTime > CurrentSegment.dt)
                        if (waypoints != null && waypoints.MoveNext())
                            AdvanceSegment();
                        else
                        {
                            LastWaypoint = CurrentSegment.Final;
                            CurrentSegmentTime -= CurrentSegment.dt;
                            CurrentSegment = null;
                        }
                    else
                        return CurrentSegment.Position(CurrentSegmentTime);
                }
                else
                {
                    if (waypoints != null && waypoints.MoveNext())
                        AdvanceSegment(); //new waypoint availabile, make a segement from here to the next waypoint  
                    else //no next waypoint, contine with the last waypoint we had  
                        return LastWaypoint.Extrapolate(CurrentSegmentTime);
                }
        }
        void AdvanceSegment()
        {
            if (CurrentSegment != null)
            {
                if (CurrentSegmentTime > CurrentSegment.dt)
                {
                    CurrentSegmentTime -= CurrentSegment.dt;
                    LastWaypoint = CurrentSegment.Final;
                }
                else
                {
                    LastWaypoint = CurrentSegment.InterpolateWaypoint(CurrentSegmentTime);
                    CurrentSegmentTime = 0;
                }
            }
            else
            {
                LastWaypoint = LastWaypoint.ExtrapolatedWaypoint(CurrentSegmentTime);
                CurrentSegmentTime = 0.0;
            }
            CurrentSegment = new PathSegment(LastWaypoint, waypoints.Current, PlannerVelocity, PlannerAcceleration);

        }
        public void Reset(Waypoint init)
        {
            LastWaypoint = init;
            CurrentSegmentTime = 0.0;
        }
    }

    /// <summary>  
    /// Class for commanding a group of IMyGyro objects.  Note: If any of the  
    /// Gyroscopes are removed/etc then the class instance should be thrown  
    /// away and a new one constructed.  
    /// </summary>  
    public class GyroGroup
    {
        List<IMyGyro> gyroscopes = null;
        string[] gyroYawField = null;
        string[] gyroPitchField = null;
        string[] gyroRollField = null;
        float[] gyroYawFactor = null;
        float[] gyroPitchFactor = null;
        float[] gyroRollFactor = null;

        static readonly string[] gyroAxisLookup = { "Roll", "Roll", "Pitch", "Pitch", "Yaw", "Yaw" };
        static readonly float[] gyroSignLookup = { -1.0f, 1.0f, 1.0f, -1.0f, 1.0f, -1.0f };

        const float RPM2RadPS = (float)(Math.PI / 30);

        /// <summary>  
        /// Constructs a GyroGroup using a list of IMyGyro objects. All gyros must have the same CubeGrid.  
        /// </summary>  
        /// <param name="gyros"></param>  
        public GyroGroup(List<IMyGyro> gyros)
        {
            gyroscopes = gyros;
            InitGyroscopes();
        }
        public void Disable() { SetGyroOverride(false); }
        public void Enable() { SetGyroOverride(true); }
        void InitGyroscopes()
        {
            gyroYawField = new string[gyroscopes.Count];
            gyroPitchField = new string[gyroscopes.Count];
            gyroYawFactor = new float[gyroscopes.Count];
            gyroPitchFactor = new float[gyroscopes.Count];
            gyroRollField = new string[gyroscopes.Count];
            gyroRollFactor = new float[gyroscopes.Count];

            for (int i = 0; i < gyroscopes.Count; i++)
            {
                int gyroUp = (int)gyroscopes[i].Orientation.Up;
                int gyroLeft = (int)gyroscopes[i].Orientation.Left;
                int gyroForward = (int)gyroscopes[i].Orientation.Forward;

                gyroYawField[i] = gyroAxisLookup[gyroUp];
                gyroYawFactor[i] = gyroSignLookup[gyroUp];
                gyroPitchField[i] = gyroAxisLookup[gyroLeft];
                gyroPitchFactor[i] = gyroSignLookup[gyroLeft];
                gyroRollField[i] = gyroAxisLookup[gyroForward];
                gyroRollFactor[i] = gyroSignLookup[gyroForward];
            }
        }
        void SetGyroOverride(bool bOverride)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                if ((gyroscopes[i]).GyroOverride != bOverride)
                    gyroscopes[i].ApplyAction("Override");
        }
        public void SetGyroYaw(float yawRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroYawField[i], -yawRate * gyroYawFactor[i]);
        }
        public void SetGyroPitch(float pitchRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroPitchField[i], pitchRate * gyroPitchFactor[i]);
        }
        public void SetGyroRoll(float rollRate)
        {
            for (int i = 0; i < gyroscopes.Count; i++)
                gyroscopes[i].SetValueFloat(gyroRollField[i], rollRate * gyroRollFactor[i]);
        }
        public void SetAngularVelocity(Vector3 w)
        {
            SetGyroPitch(w.X);
            SetGyroYaw(w.Y);
            SetGyroRoll(w.Z);
        }
        public void ResetGyro()
        {
            for (int i = 0; i < gyroscopes.Count; i++)
            {
                gyroscopes[i].SetValueFloat("Yaw", 0.0f);
                gyroscopes[i].SetValueFloat("Pitch", 0.0f);
                gyroscopes[i].SetValueFloat("Roll", 0.0f);
            }
        }

    }

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
            set { if (value) Enable(); else Disable(); _enabled = value; }
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

            return Update(errq);
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
            return Update(err);
        }
        float? Update(Quaternion err)
        {
            Vector3 axis;
            float angle;
            err.GetAxisAngle(out axis, out angle);
            Vector3 axisGrid = Vector3.TransformNormal(axis, MatrixD.Transpose(Grid.WorldMatrix.GetOrientation()));
            Vector3 cmd = -angle * Gain * axisGrid;
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
        void Command(Vector3 Command)
        {
            Vector3 DCommand = Vector3.Zero;
            if (prevCommand.HasValue) DCommand = (Command - prevCommand.Value) * UpdateFrequency;
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
            set { if (_enabled & !value) Thrusters.SetThrust(Vector3.Zero); _enabled = value; }
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
            if (Enabled)
            {
                Command(cmd);
                return err;
            }
            else return null;
        }
        Vector3? prevCommand = Vector3.Zero;
        Vector3 ICommand = Vector3.Zero;
        void Command(Vector3 Command)
        {
            Vector3 DCommand = Vector3.Zero;
            if (prevCommand.HasValue) DCommand = (Command - prevCommand.Value) * UpdateFrequency;
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

[TestClass()]
public class test_PositionPlanner
{
    [TestMethod()]
    public void test_Segment()
    {
        PositionPlanner.Waypoint init = new PositionPlanner.Waypoint(Vector3.UnitX, Vector3D.Zero);
        PositionPlanner.Waypoint final = new PositionPlanner.Waypoint(Vector3.UnitY, Vector3D.Zero);

        PositionPlanner.PathSegment ps = new PositionPlanner.PathSegment(init, final, 5, 1);

        List<Vector3D> positions = new List<Vector3D>();

        for (double t = 0.0; t < ps.dt; t += .1)
        {
            positions.Add(ps.Position(t));
        }

    }
    [TestMethod()]
    public void test_planner()
    {
        PositionPlanner pp = new PositionPlanner(new PositionPlanner.Waypoint(Vector3.UnitX, Vector3D.Zero));
        List<PositionPlanner.Waypoint> WaypointList = new List<PositionPlanner.Waypoint>();
        pp.waypoints = new StackAsEnumerator<PositionPlanner.Waypoint>(WaypointList);
        WaypointList.Add(new PositionPlanner.Waypoint(Vector3D.UnitY, Vector3D.Zero));

        List<Vector3D> positions = new List<Vector3D>();
        for (double t = 0.0; t < 20; t += .1)
        {
            positions.Add(pp.NextPosition(.1));
        }
    }
}