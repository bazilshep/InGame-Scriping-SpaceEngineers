using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll

namespace IngameScript.Control
{
    
    public class Autopilot
    {
        public Action<string> LogDelegate;
        void Log(string msg, params object[] args) { LogDelegate?.Invoke(string.Format(msg, args)); }

        public Action<string> TraceDelegate;
        void Trace(string msg, params object[] args) { TraceDelegate?.Invoke(string.Format(msg, args)); }

        IMyCubeGrid Grid;

        public PositionController PC;

        public AttitudeController AC;

        //private double UpdateFrequency = 60.0;    
        //double UpdatePeriod = 1 / 60.0;

        Func<IToolPathSegment, double, IToolPathSegment> pathplanner;
        public Func<IToolPathSegment, double, IToolPathSegment> PathPlanner
        {
            get { return pathplanner; }
            set
            {
                pathplanner = value;
                if (value != null)
                {
                    CurrentSegment = pathplanner(CurrentSegment, CurrentSegmentTime);
                }
                CurrentSegmentTime = 0;
            }
        }

        IToolPathSegment currentsegment;
        IToolPathSegment CurrentSegment
        {
            get
            {
                if (currentsegment == null) return HoldCurrentPositionSegment();
                return currentsegment;
            }
            set
            {
                currentsegment = value;
            }
        }

        private IToolPathSegment HoldCurrentPositionSegment()
        {
            return new StaticToolPathSegment(Quaternion.CreateFromRotationMatrix(Grid.WorldMatrix), Grid.WorldMatrix.Translation);
        }

        IPathSegment<Vector3D, Vector3D, PositionWaypoint> CurrentPosSegment{ get { return CurrentSegment; } }
        IPathSegment<Quaternion, Vector3, AttitudeWaypoint> CurrentAttSegment { get { return CurrentSegment; } }
        double CurrentSegmentTime;

        public Autopilot(List<IMyThrust> thrusters, List<IMyGyro> gyros, IMyCubeGrid grid)
        {
            Grid = grid;

            PC = new PositionController(thrusters, grid);
            AC = new AttitudeController(gyros, grid);

        }

        public void Update(double dt)
        {
            Vector3D? cmdP = null;
            Vector3? errP = null;
            float? errA = null;
            Quaternion? cmdA = null;
            UpdateSegment(dt);
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
            Trace("CmdP: {0}", cmdP?.ToString("0.00"));
            Trace("ErrP: {0}", errP?.ToString("0.00"));
            Trace("CmdA: {0}", cmdA?.ToStringAxisAngle("0.00"));
            Trace("ErrA: {0}", errA?.ToString("0.00"));
        }

        void UpdateSegment(double dt)
        {
            CurrentSegmentTime += dt;

            if (currentsegment == null || CurrentSegmentTime > CurrentPosSegment.dt | CurrentSegmentTime > CurrentAttSegment.dt)
                AdvanceSegment();
        }
        void AdvanceSegment()
        {
            if (PathPlanner != null)
            {
                var lastSegment = CurrentSegment;
                CurrentSegment = PathPlanner(lastSegment, CurrentSegmentTime);
                Log("Generating New Segment, Now: {0:0.0}", CurrentSegmentTime);
                if (currentsegment != null)
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
                    var finalpos = ((IPathSegment<Vector3D, Vector3D, PositionWaypoint>)lastSegment).Final;
                    var finalatt = ((IPathSegment<Quaternion, Vector3, AttitudeWaypoint>)lastSegment).Final;
                    CurrentSegment = new StaticToolPathSegment(finalatt.Position, finalpos.Position);
                    Log("New segment null, holding position");
                }
            }
            
            if (currentsegment == null)
            {
                CurrentSegment = HoldCurrentPositionSegment();
            }

            //if (currentsegment == null && Enabled)
            //{
            //    Log("currentsegment null");
            //    Enabled = false;
            //} 

            CurrentSegmentTime = 0;

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
                if (_enabled) Log("Autopilot Enable"); else Log("Autopilot Disable");
            }
        }
        void UpdateEnabled()
        {
            var pc_en = PC.Enabled;
            var ac_en = AC.Enabled;
            bool en = pc_en & ac_en;
            if (Enabled && !en)
            {
                CurrentSegment = HoldCurrentPositionSegment();
                //Enabled = false;
                if (!pc_en) Log("PC triggering hold");
                if (!ac_en) Log("AC triggering hold");
            }
        }


    }

}