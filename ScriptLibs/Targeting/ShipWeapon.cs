using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using IngameScript.Control;

namespace IngameScript.Targeting
{

    public abstract class ShipWeapon : Targetable
    {
        protected AttitudeController Controller;
        protected Vector3D WeaponVector = Vector3D.UnitZ;
        protected IMyTerminalBlock Weapon;

        public ShipWeapon(AttitudeController controller, IMyTerminalBlock weapon)
        {
            Controller = controller;
            Weapon = weapon;
            WeaponVector = Base6Directions.GetVector(Weapon.Orientation.Forward);
        }

        public override bool AimAt(Vector3D pos)
        {
            Vector3D offset = pos - Weapon.GetPosition();
            Controller.Update(WeaponVector, offset);
            return true;
        }

        //protected abstract override Trajectory3 Carrier()
        //{
        //    throw new NotImplementedException();
        //}

        //protected override IEnumerable<Trajectory1> Interceptor()
        //{
        //    throw new NotImplementedException();
        //}
    }

}
