using System;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Control
{

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

}