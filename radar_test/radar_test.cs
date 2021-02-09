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
using IngameScript.Drawing;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        IMyShipController cockpit;
        IMyTextSurface cockpit_display;
        



        public Program()
        {

            cockpit = (IMyShipController)this.GridTerminalSystem.GetBlockWithName("Fighter Cockpit");
            cockpit_display = ((IMyTextSurfaceProvider)cockpit).GetSurface(0);

        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {

        }
    }
}
