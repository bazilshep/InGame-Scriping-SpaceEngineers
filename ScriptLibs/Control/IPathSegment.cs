
namespace IngameScript.Control
{
    public interface IPathSegment<PositionT, VelocityT, WaypointT> where WaypointT : Waypoint<PositionT, VelocityT>
    {
        WaypointT Initial { get; }
        WaypointT Final { get; }
        double dt { get; }
        PositionT Position(double t);
        VelocityT Velocity(double t);
    }
}