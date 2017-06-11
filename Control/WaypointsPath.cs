using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Control
{
    
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
    
}