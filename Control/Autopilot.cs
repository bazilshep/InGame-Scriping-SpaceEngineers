using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll

namespace SpaceEngineersIngameScript.Control
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
            UpdateSegment();
            UpdateEnabled();
            if (CurrentSegment != null) {
                cmdA = CurrentAttSegment.Position(CurrentSegmentTime);
                cmdP = CurrentPosSegment.Position(CurrentSegmentTime) + Vector3D.Transform(PC.I.Translation, cmdA.Value);
            }
            if (Enabled)
            {
                errP = PC.Update(cmdP.Value);
                errA = AC.Update(cmdA.Value);
            }
            Log("CmdP: {0}", cmdP?.ToString("0.00"));
            Log("ErrP: {0}", errP?.ToString("0.00"));
            Log("CmdA: {0}", cmdA?.ToStringAxisAngle("0.00"));
            Log("ErrA: {0}", errA?.ToString("0.00"));
        }

        void UpdateSegment()
        {
            CurrentSegmentTime += UpdatePeriod;

            if (CurrentSegment == null || CurrentSegmentTime > CurrentPosSegment.dt | CurrentSegmentTime > CurrentAttSegment.dt)
                AdvanceSegment();
        }
        void AdvanceSegment()
        {
            if (PathPlanner != null)
            {
                if (CurrentSegment==null || !Enabled)
                    CurrentSegment = CoastSegment();
                CurrentSegment = PathPlanner(CurrentSegment, CurrentSegmentTime);
                Log("Generating New Segment, Now: {0:0.0}", CurrentSegmentTime);
                if (CurrentSegment != null)
                {
                    Log("New Segment:");
                    Log("Pos: {0}", CurrentPosSegment.Final.Position.ToString("0.00"));
                    Log("Vel: {0}", CurrentPosSegment.Final.Velocity.ToString("0.00"));
                    Log("");
                    Log("Fwd: {0}", CurrentAttSegment.Final.Position.Forward.ToString("0.00"));
                    Log(" Up: {0}", CurrentAttSegment.Final.Position.Up.ToString("0.00"));
                    Log("AVe: {0}", CurrentAttSegment.Final.Velocity.ToString("0.00"));
                    Log("  T: {0}", CurrentAttSegment.dt.ToString("0.000"));
                }
                else
                {
                    PathPlanner = null;
                    Log("New Segment null, disabling autopilot");
                }
            }
            else
                CurrentSegment = null;

            if (CurrentSegment == null)
                Enabled = false;

            CurrentSegmentTime = 0;

        }

        IToolPathSegment CoastSegment()
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

}