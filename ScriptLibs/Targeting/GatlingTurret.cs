using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using System.Collections.Generic;

namespace IngameScript.Targeting
{

    public class GatlingTurret : Turret
    {
        public GatlingTurret(IMyLargeGatlingTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        { yield return new Trajectory1(0.0, 400.0, 0.0, 2.0); } //large caliber bullet
    }

}
