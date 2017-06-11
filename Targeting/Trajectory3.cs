using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Targeting
{

    public struct Trajectory3
    {  //describes a 3d trajectory on the interval [0,Te]
        public Vector3D Pos;
        public Vector3D Vel;
        public Vector3D Acc;
        public double Te;
        public Trajectory3(Vector3D pos, Vector3D vel, Vector3D acc, double te)
        { Pos = pos; Vel = vel; Acc = acc; Te = te; }
        public Vector3D Eval(double dt) { return Pos + dt * Vel + .5 * dt * dt * Acc; }
        public Trajectory3 Advance(double dt)
        { return new Trajectory3(Pos + dt * Vel + .5 * dt * dt * Acc, Vel + dt * Acc, Acc, Te - dt); }
    }
    
}
