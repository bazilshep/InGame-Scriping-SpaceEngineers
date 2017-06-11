using System;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Control
{

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
    
}