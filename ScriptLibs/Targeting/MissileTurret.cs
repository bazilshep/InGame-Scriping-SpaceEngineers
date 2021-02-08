using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using System.Collections.Generic;

namespace IngameScript.Targeting
{

    public class MissileTurret : Turret
    {
        public MissileTurret(IMyLargeMissileTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        {
            yield return new Trajectory1(0.0, 100.0, 25, 4.0);
            yield return new Trajectory1(600, 200.0, 0.0, 1.0);
        } //missile
    }

}
