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


namespace SpaceEngineersIngameScript.Scripts.ToothPathProgram
{
    public class Program : MyGridProgram
    {
        //test program for AutoPilot
        //to use, rename constructor to "Program" and copy class internals and dependencies.
        Autopilot AP;

        public Program()
        {
            Echo("Init!");
            List<IMyGyro> gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);

            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);

            AP = new Autopilot(thrusters, gyros, Me.CubeGrid);

            AP.AC.SetGainZieglerNichols(60.0f, .5f);

            AP.PC.Gain = .5f;
            AP.LogDelegate = Echo;
            AP.Enabled = true;
            Echo("~Init!");
        }

        long i = 0;
        public void Main(string argument)
        {
            Echo("Main");

            if (argument != null & argument != "")
                HandleArgument(argument);
            AP.Update();
            if (Me.CubeGrid.WorldMatrix.Translation.Length() > 200)
                AP.Enabled = false;
            i++;
            Echo("~Main");
        }

        void HandleArgument(string arg)
        {
            string[] tok = arg.Split(',');
            Vector3D tgt = new Vector3D(double.Parse(tok[0]), double.Parse(tok[1]), double.Parse(tok[2]));
            Vector3D old = Me.CubeGrid.WorldMatrix.Translation;
            Vector3D up = Vector3D.UnitY;//Me.CubeGrid.WorldMatrix.Up;
            MatrixD target = MatrixD.Invert(MatrixD.CreateLookAt(Me.CubeGrid.WorldMatrix.Translation, tgt, up));
            Echo("");
            Echo(tgt.ToString());
            List<MatrixD> wp = new List<MatrixD>();
            wp.Add(target);
            target.Translation = tgt;
            wp.Add(target);
            AP.PathPlanner = new WaypointsPath(wp).GeneratePathSegment;
        }
    }
}
[TestClass()]
public class test_PositionPlanner
{
    [TestMethod()]
    public void test_Segment()
    {
        PositionWaypoint init = new PositionWaypoint(Vector3.UnitX, Vector3D.Zero);
        PositionWaypoint final = new PositionWaypoint(Vector3.UnitY, Vector3D.Zero);

        PositionPathSegment ps = new PositionPathSegment(init, final, 2);

        List<Vector3D> positions = new List<Vector3D>();

        for (double t = 0.0; t < ps.dt; t += .1)
        {
            positions.Add(ps.Position(t));
        }

    }

    [TestMethod()]
    public void test_AttSegment()
    {
        Matrix start = Matrix.CreateRotationY(.1f);
        Matrix end = Matrix.CreateRotationX(.2f);
        AttitudeWaypoint init = new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(start), Vector3D.Zero);
        AttitudeWaypoint final = new AttitudeWaypoint(Quaternion.CreateFromRotationMatrix(end), Vector3.Zero);

        AttitudePathSegment ps = new AttitudePathSegment(init, final, 2);

        List<Quaternion> positions = new List<Quaternion>();

        for (double t = 0.0; t < ps.dt; t += .1)
        {
            positions.Add(ps.Position(t));
        }

        Matrix segstart = Matrix.CreateFromQuaternion(ps.Position(0));
        Matrix segend = Matrix.CreateFromQuaternion(ps.Position(ps.dt));

        Assert.IsTrue(segstart.EqualsFast(ref start, .001f));
        Assert.IsTrue(segend.EqualsFast(ref end, .001f));
    }

    [TestMethod()]
    public void test_toolSegment()
    {
        PositionWaypoint initp = new PositionWaypoint(Vector3.UnitX, Vector3D.Zero);

        AttitudeWaypoint inita = new AttitudeWaypoint(Quaternion.Identity, Vector3D.Zero);

        MatrixD tgt = MatrixD.Invert(MatrixD.CreateLookAt(Vector3D.One, Vector3D.One * 2.0f, Vector3D.UnitY));
        Vector3D tool = Vector3.UnitX;
        ToolPathSegment seg = new ToolPathSegment(initp, inita, tgt, Vector3D.Zero, Vector3.Zero, Vector3D.UnitX,
            1, 1, 1, 1);
        IPathSegment<Quaternion, Vector3, AttitudeWaypoint> attseg = seg;
        IPathSegment<Vector3D, Vector3D, PositionWaypoint> posseg = seg;

        List<MatrixD> pos = new List<MatrixD>();
        List<Vector3D> toolpos = new List<Vector3D>();

        for (double t = 0.0; t < attseg.dt; t += attseg.dt/50.00001)
        {
            MatrixD mat = MatrixD.CreateFromQuaternion(attseg.Position(t));
            mat.Translation = posseg.Position(t);
            pos.Add(mat);
            toolpos.Add(Vector3D.Transform(tool, mat));
        }
        MatrixD err = pos[pos.Count - 1] * MatrixD.Invert(tgt);
        Assert.IsTrue(pos[pos.Count - 1].EqualsFast(ref tgt, .001));
    }

    [TestMethod()]
    public void test_stringformat_nullable()
    {
        float? nullablefloat = null;
        string s = string.Format("test: {0:0.00}", nullablefloat);
    }
}