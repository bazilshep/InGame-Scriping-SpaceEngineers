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
    class Logger
    {

        IMyTextSurface s;
        StringBuilder sb = new StringBuilder(512);

        Queue<string> lines = new Queue<string>(16);
        string lastline;

        int maxlines;

        public Logger(IMyTextSurface surf)
            : this(surf, (int)(surf.SurfaceSize.Y / surf.FontSize)) { }

        public Logger(IMyTextSurface surf, int maxlines)
        {
            s = surf;
            s.ContentType = ContentType.TEXT_AND_IMAGE;
            this.maxlines = maxlines;
        }
        public void Log(string str)
        {
            Add(str);
            Output();
        }

        void Add(string str)
        {
            var str_split = str.Split('\n');

            foreach (var line in str_split.Take(str_split.Length - 1))
            {
                if (!string.IsNullOrEmpty(lastline))
                {
                    lines.Enqueue(lastline + line);
                    lastline = null;
                }
                else
                {
                    lines.Enqueue(line);
                }
                if (lines.Count > maxlines) lines.Dequeue();
            }
            lastline += str_split[str_split.Length - 1];
        }

        private void Output()
        {

            sb.Clear();
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            sb.Append(lastline);

            s.WriteText(sb);
        }



    }
}
