using System;

namespace IngameScript.Control
{

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
            if (t < dt)
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
    
}