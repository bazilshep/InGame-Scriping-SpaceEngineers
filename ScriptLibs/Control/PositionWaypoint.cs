using VRageMath; // VRage.Math.dll

namespace IngameScript.Control
{

    public class PositionWaypoint : Waypoint<Vector3D, Vector3D>
    {
        public PositionWaypoint(Vector3D position, Vector3D velocity) : base(position, velocity) { }
        public override Vector3D Extrapolate(double dt) { return dt * Velocity + Position; }
        public override Vector3D Interpolate(Vector3D from, Vector3D to, double ratio) { return from * (1.0 - ratio) + to * ratio; }
        public override Vector3D InterpolateVelocity(Vector3D from, Vector3D fromV, Vector3D to, Vector3D toV, double ratio, double Vratio)
        { return Interpolate(fromV, toV, ratio) + Vratio * (to - from); }
    }
    
}