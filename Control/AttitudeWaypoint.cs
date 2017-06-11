using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Control
{

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
    
}