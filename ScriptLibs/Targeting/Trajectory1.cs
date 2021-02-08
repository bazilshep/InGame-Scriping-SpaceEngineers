namespace IngameScript.Targeting
{

    public struct Trajectory1
    { //describes a 1d trajectory on the interval [0,Te]
        public double Pos;
        public double Vel;
        public double Acc;
        public double Te;
        public Trajectory1(double pos, double vel, double acc, double te)
        { Pos = pos; Vel = vel; Acc = acc; Te = te; }
        public double Eval(double dt) { return Pos + dt * Vel + .5 * dt * dt * Acc; }
        public Trajectory1 Advance(double dt)
        { return new Trajectory1(Pos + dt * Vel + .5 * dt * dt * Acc, Vel + dt * Acc, Acc, Te - dt); }
    }
    
}
