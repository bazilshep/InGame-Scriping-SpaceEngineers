using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Targeting
{
    public abstract class Turret : Targetable
    {
        IMyLargeTurretBase turret;
        const double minDot = 0.0; //constraint for minimum aim elevation
        public Turret(IMyLargeTurretBase _turret) { turret = _turret; }
        protected override Trajectory3 Carrier()
        { return new Trajectory3(turret.GetPosition(), Vector3D.Zero, Vector3D.Zero, double.PositiveInfinity); }
        public override bool AimAt(Vector3D pos)
        {
            Vector3D offset = pos - turret.GetPosition();
            offset.Normalize();
            if (turret.WorldMatrix.Up.Dot(offset) >= minDot)
            {
                turret.SetTarget(pos);
                return true;
            }
            else
                return false;
        }
    }
  
}
