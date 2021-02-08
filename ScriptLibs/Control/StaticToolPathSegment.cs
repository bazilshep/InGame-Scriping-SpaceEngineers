using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;

namespace IngameScript.Control
{
    public class StaticToolPathSegment
        : IToolPathSegment
    {
        public StaticToolPathSegment(Quaternion orientation, Vector3D position)
        {
            pw = new PositionWaypoint(position, Vector3D.Zero);
            aw = new AttitudeWaypoint(orientation, Vector3.Zero);
        }
        private PositionWaypoint pw;
        private AttitudeWaypoint aw;

        PositionWaypoint IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Initial => pw;

        AttitudeWaypoint IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Initial => aw;

        PositionWaypoint IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Final => pw;

        AttitudeWaypoint IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Final => aw;

        double IPathSegment<Vector3D, Vector3D, PositionWaypoint>.dt => .001;

        double IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.dt => .001;

        void IToolPathSegment.InterpolatedWaypoint(double t, out AttitudeWaypoint att, out PositionWaypoint pos)
        {
            att = aw;
            pos = pw;
        }

        Vector3D IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Position(double t)
        {
            return pw.Position;
        }

        Quaternion IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Position(double t)
        {
            return aw.Position;
        }

        Vector3D IPathSegment<Vector3D, Vector3D, PositionWaypoint>.Velocity(double t)
        {
            return pw.Velocity;
        }

        Vector3 IPathSegment<Quaternion, Vector3, AttitudeWaypoint>.Velocity(double t)
        {
            return aw.Velocity;
        }
    }
}
