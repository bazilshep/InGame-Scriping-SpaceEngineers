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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        IMyShipController cockpit;
        IMyTextSurface _drawingSurface;
        RectangleF _viewport;
        Matrix projection;

        public Program()
        {

            cockpit = (IMyShipController)this.GridTerminalSystem.GetBlockWithName("Fighter Cockpit");
            var surf_provider = (IMyTextSurfaceProvider)this.GridTerminalSystem.GetBlockWithName("Fighter Cockpit");

            _drawingSurface = surf_provider.GetSurface(0);

            projection = Matrix.CreatePerspectiveFieldOfView(90f * 3.1415f / 180f, _viewport.Height / _viewport.Width, .01f, 100f) *
                Matrix.CreateScale(.5f * _viewport.Width, .5f * _viewport.Height, 1) *
                Matrix.CreateTranslation(.5f * _viewport.Width, .5f * _viewport.Height, 0);

        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {

        }
    }
}
