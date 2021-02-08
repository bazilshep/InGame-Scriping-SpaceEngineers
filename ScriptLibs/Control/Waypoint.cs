
namespace IngameScript.Control
{

    public abstract class Waypoint<PositionT, VelocityT>
    {
        public readonly PositionT Position;
        public readonly VelocityT Velocity;
        public Waypoint(PositionT position, VelocityT velocity)
        {
            Position = position;
            Velocity = velocity;
        }
        public abstract PositionT Extrapolate(double dt);
        public abstract PositionT Interpolate(PositionT from, PositionT to, double ratio);
        public abstract VelocityT InterpolateVelocity(PositionT from, VelocityT fromV,
                                                      PositionT to, VelocityT toV, double ratio, double Vratio);
    }
   
}