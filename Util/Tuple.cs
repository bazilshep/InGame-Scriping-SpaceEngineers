using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll
using VRage.Game; // VRage.Game.dll
using System.Text;
using Sandbox.ModAPI.Interfaces; // Sandbox.Common.dll
using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using Sandbox.Game.EntityComponents; // Sandbox.Game.dll
using VRage.Game.Components; // VRage.Game.dll
using VRage.Collections; // VRage.Library.dll
using VRage.Game.ObjectBuilders.Definitions; // VRage.Game.dll
using VRage.Game.ModAPI.Ingame; // VRage.Game.dll
using SpaceEngineers.Game.ModAPI.Ingame; // SpacenEngineers.Game.dll
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using SpaceEngineersIngameScript.Scripts;

namespace SpaceEngineersIngameScript.Util
{

    public class MyTuple<T1, T2> : IComparable<MyTuple<T1, T2>>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }

        public MyTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        private static readonly IEqualityComparer<T1> Item1EqComparer = EqualityComparer<T1>.Default;
        private static readonly IEqualityComparer<T2> Item2EqComparer = EqualityComparer<T2>.Default;

        private static readonly IComparer<T1> Item1Comparer = Comparer<T1>.Default;
        private static readonly IComparer<T2> Item2Comparer = Comparer<T2>.Default;

        public override int GetHashCode()
        {
            var hc = 0;
            if (!object.ReferenceEquals(Item1, null))
                hc = Item1EqComparer.GetHashCode(Item1);
            if (!object.ReferenceEquals(Item2, null))
                hc = (hc << 3) ^ Item2EqComparer.GetHashCode(Item2);
            return hc;
        }
        public override bool Equals(object obj)
        {
            var other = obj as MyTuple<T1, T2>;
            if (object.ReferenceEquals(other, null))
                return false;
            else
                return Item1Comparer.Compare(Item1, other.Item1) == 0 && Item2Comparer.Compare(Item2, other.Item2) == 0;
        }

        public override string ToString()
        {
            return String.Format("Tuple({0},{1})", Item1, Item2);
        }

        int IComparable<MyTuple<T1, T2>>.CompareTo(MyTuple<T1, T2> other)
        {
            int comp1 = Item1Comparer.Compare(this.Item1, other.Item1);
            if (comp1 != 0) { return comp1; }
            else
            {
                return Item2Comparer.Compare(this.Item2, other.Item2);
            }
        }
    }
  
}
