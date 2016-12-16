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
        void Log(string msg, params object[] args) { LogDelegate?.Invoke(string.Format(msg, args)); }

        IMyCubeGrid Grid;

        public PositionController PC;

        public AttitudeController AC;

        //private double UpdateFrequency = 60.0;    
        double UpdatePeriod = 1 / 60.0;

        Func<IToolPathSegment, double, IToolPathSegment> pathplanner;
        public Func<IToolPathSegment, double, IToolPathSegment> PathPlanner
        {
            get { return pathplanner; }
            set
            {
                pathplanner = value;
                CurrentSegment = pathplanner(CurrentSegment, CurrentSegmentTime);
                CurrentSegmentTime = 0;
            }
        }
        IToolPathSegment CurrentSegment;
        IPathSegment<Vector3D, Vector3D, PositionWaypoint> CurrentPosSegment { get { return CurrentSegment; } }
        IPathSegment<Quaternion, Vector3, AttitudeWaypoint> CurrentAttSegment { get { return CurrentSegment; } }
        double CurrentSegmentTime;

        public Autopilot(List<IMyThrust> thrusters, List<IMyGyro> gyros, IMyCubeGrid grid)
        {
            Grid = grid;

            PC = new PositionController(thrusters, grid);
            AC = new AttitudeController(gyros, grid);

        }

        public void Update()
        {
            Vector3D? cmdP = null;
            Vector3? errP = null;
            float? errA = null;
            Quaternion? cmdA = null;
            UpdateEnabled();
            UpdateSegment();
            cmdA = CurrentAttSegment.Position(CurrentSegmentTime);
            cmdP = CurrentPosSegment.Position(CurrentSegmentTime) + Vector3D.Transform(PC.I.Translation, cmdA.Value);
            if (Enabled)
            {
                errP = PC.Update(cmdP.Value);
                errA = AC.Update(cmdA.Value);
            }
            Log("CmdP: [{0:0.00}]", cmdP);
            Log("ErrP: [{0:0.00}]", errP);
            Log("CmdA: [{0:0.00}]", cmdA);
            Log("ErrA: [{0:0.00}]", errA);
        }

        void UpdateSegment()
        {
            CurrentSegmentTime += UpdatePeriod;
            if (CurrentSegment == null || CurrentSegmentTime > CurrentPosSegment.dt | CurrentSegmentTime > CurrentAttSegment.dt)
            {
                Log("Generating Next Segment, Now: {0:0.0}", CurrentSegmentTime);
                if (PathPlanner != null)
                {
                    IToolPathSegment NextSegment = PathPlanner(CurrentSegment, CurrentSegmentTime);
                    if (NextSegment == null)
                    {
                        CurrentSegment = stopSegment();
                        pathplanner = null;
                    }
                    else
                        CurrentSegment = NextSegment;
                }
                else
                    CurrentSegment = stopSegment();
                CurrentSegmentTime = 0;
                Log("New Segment:");
                Log("Pos: [{0:0.00}]", CurrentPosSegment.Final.Position);
                Log("Vel: [{0:0.00}]", CurrentPosSegment.Final.Velocity);
                Log("");
                Log("Fwd: [{0:0.00}]", CurrentAttSegment.Final.Position.Forward);
                Log(" Up: [{0:0.00}]", CurrentAttSegment.Final.Position.Up);
                Log("AVe: [{0:0.00}]", CurrentAttSegment.Final.Velocity);
                Log("  T:   {0:0.000}", CurrentAttSegment.dt);
            }
        }

        IToolPathSegment stopSegment()
        {
            Log("Generating Stop Segment");
            return new ToolPathSegment(new PositionWaypoint(Grid.WorldMatrix.Translation, Vector3D.Zero),
                new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(Grid.WorldMatrix), Vector3D.Zero),
                Grid.WorldMatrix, Vector3D.Zero, Vector3.Zero, Vector3D.Zero, .000001, .000001, .000001, .000001);
        }

        bool _enabled = false;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                PC.Enabled = value;
                AC.Enabled = value;
                _enabled = value;
            }
        }
        void UpdateEnabled()
        {
            bool en = PC.Enabled & AC.Enabled;
            if (en != Enabled) //only call the setter if it changed, to avoid O(n) cost    
                Enabled = en;
        }
    }

    public class WaypointsPath
    {
        Vector3D ToolPoint = Vector3D.Zero;
        List<MatrixD> Waypoints;
        int currentWaypoint = 0;
        public double MaxMidVelocity = 20;
        public double MaxAcceleration = 5;
        public double MaxMidAngularVelocity = 2;
        public double MaxAngularAcceleration = 3;
        public WaypointsPath(List<MatrixD> waypoints)
        {
            Waypoints = new List<MatrixD>(waypoints);
        }
        public IToolPathSegment GeneratePathSegment(IToolPathSegment last, double lastT)
        {
            if (currentWaypoint >= Waypoints.Count)
                return null;
            MatrixD target = Waypoints[currentWaypoint++];
            PositionWaypoint lastp;
            AttitudeWaypoint lasta;
            last.InterpolatedWaypoint(lastT, out lasta, out lastp);
            return new ToolPathSegment(lastp, lasta, target, Vector3D.Zero, Vector3.Zero, ToolPoint,
                MaxMidVelocity, MaxAcceleration, MaxMidAngularVelocity, MaxAngularAcceleration);
        }
    }

    public abstract class Waypoint<PositionT, VelocityT>
    {
        public readonly PositionT Position;
        public readonly VelocityT Velocity;
        public Waypoint(PositionT position, VelocityT velocity)
        {
            Position = position;
            Velocity = velocity;
        }
        public abstract PositionT Extrapolate(double dt);
        public abstract PositionT Interpolate(PositionT from, PositionT to, double ratio);
        public abstract VelocityT InterpolateVelocity(PositionT from, VelocityT fromV,
                                                      PositionT to, VelocityT toV, double ratio, double Vratio);
    }

    public class PositionWaypoint : Waypoint<Vector3D, Vector3D>
    {
        public PositionWaypoint(Vector3D position, Vector3D velocity) : base(position, velocity) { }
        public override Vector3D Extrapolate(double dt) { return dt * Velocity + Position; }
        public override Vector3D Interpolate(Vector3D from, Vector3D to, double ratio) { return from * (1.0 - ratio) + to * ratio; }
        public override Vector3D InterpolateVelocity(Vector3D from, Vector3D fromV, Vector3D to, Vector3D toV, double ratio, double Vratio)
        { return Interpolate(fromV, toV, ratio) + Vratio * (to - from); }
    }

    public class AttitudeWaypoint : Waypoint<Quaternion, Vector3>
    {
        readonly Vector3 AngularVelocityAxis;
        readonly float AngularVelocity;
        public AttitudeWaypoint(Quaternion position, Vector3 velocity) : base(position, velocity)
        {
            AngularVelocityAxis = Velocity;
            if (AngularVelocityAxis.Length() > 0.0)
                AngularVelocityAxis.Normalize();
            AngularVelocity = Velocity.Length();
        }
        public override Quaternion Extrapolate(double dt) { return Quaternion.CreateFromAxisAngle(AngularVelocityAxis, AngularVelocity * (float)dt) * Position; }
        public override Quaternion Interpolate(Quaternion from, Quaternion to, double ratio) { return Quaternion.Slerp(from, to, (float)ratio); }
        public override Vector3 InterpolateVelocity(Quaternion from, Vector3 fromV, Quaternion to, Vector3 toV, double ratio, double Vratio)
        {
            return fromV * (float)(1.0 - ratio) + toV * (float)ratio + quaternionDifference(to, from) * (float)Vratio;
        }
        public static Vector3 quaternionDifference(Quaternion a, Quaternion b)
        {
            Vector3 difference;
            float ang;
            (a / b).GetAxisAngle(out difference, out ang);
            difference *= ang;
            return difference;
        }
    }

    public interface IPathSegment<PositionT, VelocityT, WaypointT> where WaypointT : Waypoint<PositionT, VelocityT>
    {
        WaypointT Initial { get; }
        WaypointT Final { get; }
        double dt { get; }
        PositionT Position(double t);
        VelocityT Velocity(double t);
    }

    public interface IToolPathSegment : IPathSegment<Vector3D, Vector3D, PositionWaypoint>, IPathSegment<Quaternion, Vector3, AttitudeWaypoint>
    {
        void InterpolatedWaypoint(double t, out AttitudeWaypoint att, out PositionWaypoint pos);
    }

    public class CosinePathSegment<PositionT, VelocityT, WaypointT>
        : IPathSegment<PositionT, VelocityT, WaypointT>
        where WaypointT : Waypoint<PositionT, VelocityT>
    {
        public readonly WaypointT Initial;
        public readonly WaypointT Final;
        public readonly double dt;

        public CosinePathSegment(WaypointT initial, WaypointT final, double t)
        {
            Initial = initial;
            Final = final;
            dt = t;
        }
        public PositionT Position(double t)
        {
            if (t < dt)
                return Initial.Interpolate(Initial.Extrapolate(t), Final.Extrapolate(t - dt), Interp(t));
            else
                return Final.Extrapolate(t - dt);
        }
        public VelocityT Velocity(double t)
        {
            if (t > dt)
                return Initial.InterpolateVelocity(Initial.Extrapolate(t), Initial.Velocity,
                    Final.Extrapolate(dt - t), Final.Velocity,
                    Interp(t), dInterp(t));
            else
                return Final.Velocity;
        }

        WaypointT IPathSegment<PositionT, VelocityT, WaypointT>.Initial { get { return Initial; } }
        WaypointT IPathSegment<PositionT, VelocityT, WaypointT>.Final { get { return Final; } }
        double IPathSegment<PositionT, VelocityT, WaypointT>.dt { get { return dt; } }

        protected double Interp(double t) { return (1.0 - Math.Cos(Math.PI * t / dt)) * .5; }
        protected double dInterp(double t) { double c = Math.PI / dt; return 0.5 * c * Math.Sin(c * t); }
        protected double ddInterp(double t) { double c = Math.PI / dt; return 0.5 * c * c * Math.Cos(c * t); }
    }

    public class PositionPathSegment : CosinePathSegment<Vector3D, Vector3D, PositionWaypoint>
    {
        public PositionPathSegment(PositionWaypoint initial, PositionWaypoint final, double t) : base(initial, final, t) { }

        public static double CalculateTime(PositionWaypoint initial, PositionWaypoint final, double MaxMidVelocity, double MidAcceleration)
        {
            Vector3D dP = final.Position - initial.Position;
            Vector3D dV = final.Velocity - initial.Velocity;
            double AccelDist = 0.5 * dP.Length();
            double v = (final.Velocity + initial.Velocity).Dot(dP) / dP.Length();
            double PeakMidVelocity = Math.Min(MaxMidVelocity, Math.Sqrt(Math.Abs(AccelDist * MidAcceleration) + v * v));
            double AvgVelocity = .5 * (v + PeakMidVelocity);
            return (AccelDist > 0) ? 2 * AccelDist / AvgVelocity : .001;
        }
        public Vector3D Acceleration(double t)
        {
            return 2.0 * dInterp(t) * Final.Velocity - Initial.Velocity +
                ddInterp(t) * (Final.Extrapolate(t - dt) - Initial.Extrapolate(t));
        }
    }

    public class AttitudePathSegment : CosinePathSegment<Quaternion, Vector3, AttitudeWaypoint>
    {
        public AttitudePathSegment(AttitudeWaypoint initial, AttitudeWaypoint final, double t) : base(initial, final, t) { }
        public static double CalculateTime(AttitudeWaypoint initial, AttitudeWaypoint final, double MaxMidVelocity, double MidAcceleration)
        {
            Vector3 dP = AttitudeWaypoint.quaternionDifference(final.Position, initial.Position);
            Vector3 dV = final.Velocity - initial.Velocity;
            double AccelDist = 0.5 * dP.Length();
            double v = (final.Velocity + initial.Velocity).Dot(dP) / dP.Length();
            double PeakMidVelocity = Math.Min(MaxMidVelocity, Math.Sqrt(Math.Abs(AccelDist * MidAcceleration) + v * v));
            double AvgVelocity = .5 * (v + PeakMidVelocity);
            return (AccelDist > 0) ? 2 * AccelDist / AvgVelocity : .001;
        }
        public Vector3D Acceleration(double t)
        {
            return 2.0f * (float)dInterp(t) * (Final.Velocity - Initial.Velocity) + (float)ddInterp(t) *
                AttitudeWaypoint.quaternionDifference(Final.Extrapolate(t - dt), Initial.Extrapolate(t));
        }
    }

    public class ToolPathSegment : IToolPathSegment
    {
        public ToolPathSegment(PositionWaypoint lastP, AttitudeWaypoint lastA,
            MatrixD target, Vector3D Velocity, Vector3 AngularVelocity, Vector3D toolOffset,
            double maxMidVel, double maxAcc, double maxMidAngVel, double maxAngAcc)
        {
            //Velocity is velocity of control point    
            //target is grid world matrix    
            //lastP is last waypoint of grid origin    

            ToolOffset = toolOffset;
            PositionWaypoint toolLastP = ToolWaypoint(lastP, lastA, ToolOffset);

            AttitudeWaypoint nextA;
            TargetWaypoints(target, Velocity, AngularVelocity, ToolOffset, out nextA, out OriginFinal);
            OriginInitial = lastP;

            PositionWaypoint toolnextP = ToolWaypoint(OriginFinal, nextA, ToolOffset);

            double tt = TransitTime(lastA, toolLastP,
                nextA, toolnextP, maxMidVel, maxAcc, maxMidAngVel, maxAngAcc);
            att = new AttitudePathSegment(lastA, nextA, tt);
            toolPath = new PositionPathSegment(toolLastP, toolnextP, tt);
        }

        /// <summary>    
        /// Calculates the AttitudeWaypoint and PositionWaypoint (Grid Origin) for the given     
        /// </summary>    
        /// <param name="target">Target worldMatrix</param>    
        /// <param name="Velocity">Velocity of Tool</param>    
        /// <param name="AngularVelocity">Target Angular velocity of grid</param>    
        /// <param name="ToolOffset">Position of tool point in Grid Coordinates</param>    
        /// <param name="waypta">Attitude Waypoint</param>    
        /// <param name="wayptp">Position Waypoint (of grid origin)</param>    
        public static void TargetWaypoints(MatrixD target, Vector3D Velocity, Vector3 AngularVelocity, Vector3D ToolOffset, out AttitudeWaypoint waypta, out PositionWaypoint wayptp)
        {
            waypta = new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(target), AngularVelocity);
            Vector3D wToolOffset = Vector3D.TransformNormal(ToolOffset, target);
            Vector3D OriginVelocity = OriginVelocityOffset(AngularVelocity, wToolOffset) + Velocity;
            wayptp = new PositionWaypoint(target.Translation, OriginVelocity);
        }

        public void InterpolatedWaypoint(double dt, out AttitudeWaypoint waypta, out PositionWaypoint wayptp)
        {
            if (att.dt >= dt)
                waypta = new AttitudeWaypoint(att.Position(dt), att.Velocity(dt));
            else
                waypta = new AttitudeWaypoint(att.Final.Extrapolate(dt - att.dt), att.Final.Velocity);

            PositionWaypoint toolWayPt;
            if (toolPath.dt >= dt)
                toolWayPt = new PositionWaypoint(toolPath.Position(dt), toolPath.Velocity(dt));
            else
                toolWayPt = new PositionWaypoint(toolPath.Final.Extrapolate(dt - toolPath.dt), toolPath.Final.Velocity);
            wayptp = OriginWaypoint(toolWayPt, waypta, ToolOffset);
        }

        public static double TransitTime(AttitudeWaypoint lasta, PositionWaypoint lastp, AttitudeWaypoint tgta, PositionWaypoint tgtp,
                                  double maxMidVel, double maxAcc, double maxMidAngVel, double maxAngAcc)
        {
            double tp = PositionPathSegment.CalculateTime(lastp, tgtp, maxMidVel, maxAcc);
            double ta = AttitudePathSegment.CalculateTime(lasta, tgta, maxMidAngVel, maxAngAcc);
            return Math.Max(tp, ta);
        }

        //IAttitudePathSegment    
        protected IPathSegment<Quaternion, Vector3, AttitudeWaypoint> att;
        double IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.dt { get { return att.dt; } }
        AttitudeWaypoint IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Final { get { return att.Final; } }
        AttitudeWaypoint IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Initial { get { return att.Initial; } }
        Quaternion IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Position(double t)
        {
            return att.Position(t);
        }
        Vector3 IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Velocity(double t) { return att.Velocity(t); }

        protected PositionWaypoint OriginInitial; //grid origin endpoint waypoints    
        protected PositionWaypoint OriginFinal;
        Vector3D ToolOffset; //offset from grid origin to control point, in grid coordinates    
        protected IPathSegment<Vector3D, Vector3D, PositionWaypoint> toolPath;
        //IPositionPathSegment, of grid origin    
        double IPathSegment<Vector3D, Vector3D, PositionWaypoint>.dt { get { return toolPath.dt; } }
        PositionWaypoint IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Final { get { return OriginFinal; } }
        PositionWaypoint IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Initial { get { return OriginInitial; } }
        Vector3D IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Position(double t)
        {
            return toolPath.Position(t) + OriginOffset(att.Position(t), ToolOffset);
        }
        Vector3D IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Velocity(double t)
        {
            return toolPath.Velocity(t) + OriginVelocityOffset(att.Velocity(t), OriginOffset(att.Position(t), ToolOffset));
        }

        static Vector3D OriginVelocityOffset(Vector3 angvel, Vector3D WToolOffset) { return Vector3D.Cross(angvel, WToolOffset); } //origin velocity - tool velocity in world coords    
        static Vector3D OriginOffset(Quaternion rot, Vector3D ToolOffset) { return Vector3D.Transform(-ToolOffset, rot); } //offset from tool to origin in world coords    
        static PositionWaypoint ToolWaypoint(PositionWaypoint OriginWaypoint, AttitudeWaypoint attitude, Vector3D ToolOffset)
        { //convert origin waypoint to tool waypoint    
            Vector3D WOffset = OriginOffset(attitude.Position, ToolOffset);
            return new PositionWaypoint(OriginWaypoint.Position - WOffset,
                OriginWaypoint.Velocity - OriginVelocityOffset(attitude.Velocity, WOffset));
        }
        static PositionWaypoint OriginWaypoint(PositionWaypoint ToolWaypoint, AttitudeWaypoint attitude, Vector3D ToolOffset)
        { //convert tool waypoint to origin waypoint    
            Vector3D WOffset = OriginOffset(attitude.Position, ToolOffset);
            return new PositionWaypoint(ToolWaypoint.Position + OriginVelocityOffset(attitude.Velocity, WOffset),
                ToolWaypoint.Velocity + OriginVelocityOffset(attitude.Velocity, WOffset));
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
                MatrixD refMatrix = gyroscopes[i].CubeGrid.WorldMatrix;
                int gyroUp = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Up);
                int gyroLeft = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Left);
                int gyroForward = (int)gyroscopes[i].WorldMatrix.GetClosestDirection(refMatrix.Forward);

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
            set { if (!value) Thrusters.SetThrust(Vector3.Zero); _enabled = value; }
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

public class ToolPathProgram : MyGridProgram
{
    //test program for AutoPilot
    //to use, rename constructor to "Program" and copy class internals and dependencies.
    Autopilot AP;

    public ToolPathProgram()
    {
        Echo("Init!");
        List<IMyGyro> gyros = new List<IMyGyro>();
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);

        List<IMyThrust> thrusters = new List<IMyThrust>();
        GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

        AP = new Autopilot(thrusters, gyros, Me.CubeGrid);

        AP.AC.SetGainZieglerNichols(60.0f, .5f);

        AP.PC.Gain = .5f;
        AP.LogDelegate = Echo;
        AP.Enabled = true;
        Echo("~Init!");
    }

    long i = 0;
    public void Main(string argument)
    {
        Echo("Main");

        if (argument != null & argument != "")
            HandleArgument(argument);
        AP.Update();
        if (Me.CubeGrid.WorldMatrix.Translation.Length() > 200)
            AP.Enabled = false;
        i++;
        Echo("~Main");
    }

    void HandleArgument(string arg)
    {
        string[] tok = arg.Split(',');
        Vector3D tgt = new Vector3D(double.Parse(tok[0]), double.Parse(tok[1]), double.Parse(tok[2]));
        Vector3D old = Me.CubeGrid.WorldMatrix.Translation;
        Vector3D up = Vector3D.UnitY;//Me.CubeGrid.WorldMatrix.Up;
        MatrixD target = MatrixD.Invert(MatrixD.CreateLookAt(Me.CubeGrid.WorldMatrix.Translation, tgt, up));
        Echo("");
        Echo(tgt.ToString());
        List<MatrixD> wp = new List<MatrixD>();
        wp.Add(target);
        target.Translation = tgt;
        wp.Add(target);
        AP.PathPlanner = new WaypointsPath(wp).GeneratePathSegment;
    }
}

[TestClass()]
public class test_PositionPlanner
{
    [TestMethod()]
    public void test_Segment()
    {
        PositionWaypoint init = new PositionWaypoint(Vector3.UnitX, Vector3D.Zero);
        PositionWaypoint final = new PositionWaypoint(Vector3.UnitY, Vector3D.Zero);

        PositionPathSegment ps = new PositionPathSegment(init, final, 2);

        List<Vector3D> positions = new List<Vector3D>();

        for (double t = 0.0; t < ps.dt; t += .1)
        {
            positions.Add(ps.Position(t));
        }

    }

    [TestMethod()]
    public void test_AttSegment()
    {
        Matrix start = Matrix.CreateRotationY(.1f);
        Matrix end = Matrix.CreateRotationX(.2f);
        AttitudeWaypoint init = new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(start), Vector3D.Zero);
        AttitudeWaypoint final = new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(end), Vector3.Zero);

        AttitudePathSegment ps = new AttitudePathSegment(init, final, 2);

        List<Quaternion> positions = new List<Quaternion>();

        for (double t = 0.0; t < ps.dt; t += .1)
        {
            positions.Add(ps.Position(t));
        }

        Matrix segstart = Matrix.CreateFromQuaternion(ps.Position(0));
        Matrix segend = Matrix.CreateFromQuaternion(ps.Position(ps.dt));

        Assert.IsTrue(segstart.EqualsFast(ref start, .001f));
        Assert.IsTrue(segend.EqualsFast(ref end, .001f));
    }

    [TestMethod()]
    public void test_toolSegment()
    {
        PositionWaypoint initp = new PositionWaypoint(Vector3.UnitX, Vector3D.Zero);

        AttitudeWaypoint inita = new AttitudeWaypoint(Quaternion.Identity, Vector3D.Zero);

        MatrixD tgt = MatrixD.Invert(MatrixD.CreateLookAt(Vector3D.One, Vector3D.One * 2.0f, Vector3D.UnitY));
        Vector3D tool = Vector3.UnitX;
        ToolPathSegment seg = new ToolPathSegment(initp, inita, tgt, Vector3D.Zero, Vector3.Zero, Vector3D.UnitX,
            1, 1, 1, 1);
        IPathSegment<Quaternion, Vector3, AttitudeWaypoint> attseg = seg;
        IPathSegment<Vector3D, Vector3D, PositionWaypoint> posseg = seg;

        List<MatrixD> pos = new List<MatrixD>();
        List<Vector3D> toolpos = new List<Vector3D>();

        for (double t = 0.0; t < attseg.dt; t += attseg.dt/50.00001)
        {
            MatrixD mat = MatrixD.CreateFromQuaternion(attseg.Position(t));
            mat.Translation = posseg.Position(t);
            pos.Add(mat);
            toolpos.Add(Vector3D.Transform(tool, mat));
        }
        MatrixD err = pos[pos.Count - 1] * MatrixD.Invert(tgt);
        Assert.IsTrue(pos[pos.Count - 1].EqualsFast(ref tgt, .001));
    }

    [TestMethod()]
    public void test_stringformat_nullable()
    {
        float? nullablefloat = null;
        string s = string.Format("test: {0:0.00}", nullablefloat);
    }
}