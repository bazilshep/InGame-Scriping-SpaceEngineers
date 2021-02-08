using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using System.Collections.Generic;

namespace IngameScript.Targeting
{

    public class InteriorTurret : Turret
    {
        public InteriorTurret(IMyLargeInteriorTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        { yield return new Trajectory1(0.0, 300.0, 0.0, 8.0 / 3.0); } //small caliber bullet
    }

}
