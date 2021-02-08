using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace test_util
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

            var t = Vector3.Transform(new Vector3(1, 1, 1), Matrix.CreateScale(2) * Matrix.CreateTranslation(1, 0, 0));
            

            var s = new IngameScript.Drawing.Frame3D();
            var c = new IngameScript.Drawing.Cube();

            var viewport = new RectangleF(0, 0, 100, 100);

            var projection = Matrix.CreatePerspectiveFieldOfView(.7f, viewport.Height / viewport.Width, .01f, 100f) *
                Matrix.CreateScale(viewport.Width, viewport.Height, 1) *
                Matrix.CreateTranslation(.5f * viewport.Width, .5f * viewport.Height, 0);

            s.Clear();
            c.trf = Matrix.CreateLookAt(new Vector3(0, 0, -5), new Vector3(5, 1, .01), Vector3.UnitZ);
            c.AddTo(s, projection);

        }
    }
}
