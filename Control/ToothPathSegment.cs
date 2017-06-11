using System;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Control
{

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
    
}