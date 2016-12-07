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

namespace SpaceEngineersIngameScript.Scripts
{

    public static class PMath
    {
        static MatrixD companionmatrix(ref Vector4D poly)
        {
            MatrixD m = default(MatrixD);
            m.M12 = 1.0;
            m.M23 = 1.0;
            m.M34 = 1.0;
            m.M41 = -poly.X;
            m.M42 = -poly.Y;
            m.M43 = -poly.Z;
            m.M44 = -poly.W;
            return m;
        }
        static Vector4D eigval(MatrixD m)
        { //computes the approximate eigenvalues of the matrix m
            Vector4D rtn = default(Vector4D);
            for (int i = 0; i < 4; i++)
            {
                MyTuple<double, Vector4D> eig = largestEig(m);
                Vector4DSet(i, eig.Item1, ref rtn);
                Vector4D v = eig.Item2;
                MatrixD ex = default(MatrixD);
                ex.M11 = 1.0 - v.X * v.X;
                ex.M22 = 1.0 - v.Y * v.Y;
                ex.M33 = 1.0 - v.Z * v.Z;
                ex.M44 = 1.0 - v.W * v.W;

                ex.M21 = -v.Y * v.X; ex.M12 = ex.M21;
                ex.M31 = -v.Z * v.X; ex.M13 = ex.M31;
                ex.M41 = -v.W * v.X; ex.M14 = ex.M41;

                ex.M32 = -v.Z * v.Y; ex.M23 = ex.M32;
                ex.M42 = -v.W * v.Y; ex.M24 = ex.M42;

                ex.M43 = -v.W * v.Z; ex.M34 = ex.M43;
                m = ex * m;
            }
            return rtn;
        }
        static MyTuple<double, Vector4D> largestEig(MatrixD m)
        {
            //iterative eigenvalue solver                
            //this will find the largest eigenvalue of the coefficent matrix
            Vector4D eigvec = Vector4D.One;
            for (int i = 0; i < 10; i++)
            {
                eigvec = Vector4D.Transform(eigvec, m);
                eigvec = eigvec / eigvec.Length();
            }
            double eigval = Vector4D.Transform(eigvec, m).X / eigvec.X;
            //largest solution to the polynomial equation sum( coeff[k]*s^k )                
            return new MyTuple<double, Vector4D>(eigval, eigvec);
        }
        public static Vector4D poly4roots(ref Vector4D coeff)
        {   //this solves the quartic equation using an iterative method
            //first, find approximate roots by finding the approximate
            //eigenvalues of the polynomial's companion matrix
            Vector4D roots = eigval(companionmatrix(ref coeff));
            for (int i = 0; i < 4; i++)
            {
                //then perform a second-order newton's method
                //to improve the accuracy
                double x = Vector4DGet(i, ref roots);
                for (int j = 0; j < 5; j++)
                {
                    double p = poly4(ref coeff, x);
                    double dp = dpoly4(ref coeff, x);
                    double ddp = ddpoly4(ref coeff, x);

                    double d = dp * dp - 4.0 * p * ddp;
                    if (d < 0) { x += -.5 * dp / ddp; }
                    else if (dp < 0) { x += .5 * (-dp - Math.Sqrt(d)) / ddp; }
                    else { x += .5 * (-dp + Math.Sqrt(d)) / ddp; }
                }

                Vector4DSet(i, x, ref roots);
            }
            return roots;
        }
        public static void Vector4DSet(int i, double k, ref Vector4D v)
        {
            if (i == 0) { v.X = k; }
            else if (i == 1) { v.Y = k; }
            else if (i == 2) { v.Z = k; }
            else if (i == 3) { v.W = k; }
        }
        public static double Vector4DGet(int i, ref Vector4D v)
        {
            if (i == 0) { return v.X; }
            else if (i == 1) { return v.Y; }
            else if (i == 2) { return v.Z; }
            else return v.W;
        }
        public static double poly4(ref Vector4D p, double x)
        {//evaluates a 4th degree polynomial with coefficients 0to3 in p, and the 4th coeff 1
            return p.X + x * (p.Y + x * (p.Z + x * (p.W + x)));
        }
        public static double dpoly4(ref Vector4D p, double x)
        {//evaluates a 4th degree polynomial's derivative
            return p.Y + x * (2.0 * p.Z + x * (3.0 * p.W + 4.0 * x));
        }
        public static double ddpoly4(ref Vector4D p, double x)
        {//evaluates a 4th degree polynomial's second derivative
            return 2.0 * p.Z + x * (6.0 * p.W + 12.0 * x);
        }
    }

    public struct Trajectory3
    {  //describes a 3d trajectory on the interval [0,Te]
        public Vector3D Pos;
        public Vector3D Vel;
        public Vector3D Acc;
        public double Te;
        public Trajectory3(Vector3D pos, Vector3D vel, Vector3D acc, double te)
        { Pos = pos; Vel = vel; Acc = acc; Te = te; }
        public Vector3D Eval(double dt) { return Pos + dt * Vel + .5 * dt * dt * Acc; }
        public Trajectory3 Advance(double dt)
        { return new Trajectory3(Pos + dt * Vel + .5 * dt * dt * Acc, Vel + dt * Acc, Acc, Te - dt); }
    }

    public struct Trajectory1
    { //describes a 1d trajectory on the interval [0,Te]
        public double Pos;
        public double Vel;
        public double Acc;
        public double Te;
        public Trajectory1(double pos, double vel, double acc, double te)
        { Pos = pos; Vel = vel; Acc = acc; Te = te; }
        public double Eval(double dt) { return Pos + dt * Vel + .5 * dt * dt * Acc; }
        public Trajectory1 Advance(double dt)
        { return new Trajectory1(Pos + dt * Vel + .5 * dt * dt * Acc, Vel + dt * Acc, Acc, Te - dt); }
    }

    public abstract class Targetable
    {
        protected double tol = .1;
        protected abstract IEnumerable<Trajectory1> Interceptor();
        protected abstract Trajectory3 Carrier();
        public abstract bool AimAt(Vector3D pos);
        public bool AimAt(double t, IEnumerable<Trajectory3> target, double tol)
        {
            Vector3? tgt = InterceptPosition(target);
            if (tgt.HasValue) { AimAt(tgt.Value); return true; } else return false;
        }
        private Vector3D? InterceptPosition(IEnumerable<Trajectory3> target)
        {
            IEnumerator<Trajectory1> interceptorEn = Interceptor().GetEnumerator();
            Trajectory3 inertial = Carrier();
            IEnumerator<Trajectory3> targetEn = target.GetEnumerator();

            bool cont = true;

            cont &= interceptorEn.MoveNext();
            cont &= targetEn.MoveNext();

            Trajectory1 interceptori = interceptorEn.Current;
            Trajectory3 targeti = targetEn.Current;
            double ti = 0;

            while (cont)
            {
                double tmax = Math.Min(interceptori.Te, targeti.Te);
                double? soln = InterceptTime(targetEn.Current, inertial, interceptorEn.Current, tol);
                if (soln.HasValue && soln.Value <= (tmax + ti))
                    return targeti.Eval(soln.Value);
                else
                {
                    ti += tmax;
                    if (!(interceptori.Te > tmax))
                    {
                        if (interceptorEn.MoveNext())
                            interceptori = interceptorEn.Current;
                        else
                            cont = false;
                    }
                    else interceptori = interceptori.Advance(tmax);
                    if (!(targeti.Te == tmax))
                    {
                        if (targetEn.MoveNext())
                            targeti = targetEn.Current;
                        else
                            cont = false;
                    }
                    else targeti = targeti.Advance(tmax);
                }
            }
            return null;
        }
        private static double? InterceptTime(Trajectory3 target, Trajectory3 inertial, Trajectory1 interceptor, double tol)
        {
            //Calculates the time to intercept of a target and interceptor undergoing constant acceleration
            //Solves the equation below, which is the intercept equation between two accelerating bodies                              
            //targetTrajectory(t) = inertialTrajectory(t) + u*interceptorTrajectory(t) where u is a unit vector
            //this is done by transforming it into the equation into the form below:
            //(targetTrajectory(t)-inertialTrajectory(t))^2 = u^2*interceptorTrajectory(t)^2
            //(targetTrajectory(t)-inertialTrajectory(t))^2 - interceptorTrajectory(t)^2 = 0
            //                
            //this is intersecting a sphere (defined by the position reachable by the interceptor) and a                
            //3d parabola (trajectory of the target)

            Vector3D pos = target.Pos - inertial.Pos;
            Vector3D vel = target.Vel - inertial.Vel;
            Vector3D acc = target.Acc - inertial.Acc;

            //we need a polynomial with a nonzero leading coefficient, so let s=1/t, and solve the polynomial
            //in s instead. This gives us a nonzero leading coefficient if the target range is nonzero.
            double leadingCoeff = (pos.Dot(pos) - interceptor.Pos * interceptor.Pos);
            Vector4D coeff = new Vector4D(
                        (.25 * (acc.Dot(acc) - interceptor.Acc * interceptor.Acc)) / leadingCoeff,
                        (vel.Dot(acc) - interceptor.Vel * interceptor.Acc) / leadingCoeff,
                        (vel.Dot(vel) - interceptor.Vel * interceptor.Vel + pos.Dot(acc) - interceptor.Pos * interceptor.Acc) / leadingCoeff,
                        (2.0 * (pos.Dot(vel) - interceptor.Pos * interceptor.Vel)) / leadingCoeff
                       );
            //approximately solve the quartic equation using an iterative method
            //this only returns real approximations to the solutions, if a solution
            //is imaginary it will return the equation minima instead.
            Vector4D s = PMath.poly4roots(ref coeff);

            //now find the solution we want out of the 4 availible
            double t = double.PositiveInfinity;
            double bestErr = double.PositiveInfinity;
            for (int i = 0; i < 4; i++)
            {
                double si = PMath.Vector4DGet(i, ref s);
                double ti = 1 / si;
                double err = Math.Abs(PMath.poly4(ref coeff, si)) / (si * si * si * si);
                if ((err < tol & ti > 0 & ti < t) | (bestErr > tol & ti > 0 & err < bestErr))
                {
                    t = ti;
                    bestErr = err;
                }
            }
            if (bestErr < tol & t < target.Te & t < interceptor.Te & t < inertial.Te)
            {
                return t;
            }
            return null;
        }
    }

    public abstract class Turret : Targetable
    {
        IMyLargeTurretBase turret;
        const double minDot = 0.0; //constraint for minimum aim elevation
        public Turret(IMyLargeTurretBase _turret) { turret = _turret; }
        protected override Trajectory3 Carrier()
        { return new Trajectory3(turret.GetPosition(), Vector3D.Zero, Vector3D.Zero, double.PositiveInfinity); }
        public override bool AimAt(Vector3D pos)
        {
            Vector3D offset = pos - turret.GetPosition();
            offset.Normalize();
            if (turret.WorldMatrix.Up.Dot(offset) >= minDot)
            {
                turret.SetTarget(pos);
                return true;
            }
            else
                return false;
        }
    }

    public class MissileTurret : Turret
    {
        public MissileTurret(IMyLargeMissileTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        {
            yield return new Trajectory1(0.0, 100.0, 25, 4.0);
            yield return new Trajectory1(600, 200.0, 0.0, 1.0);
        } //missile
    }

    public class GatlingTurret : Turret
    {
        public GatlingTurret(IMyLargeGatlingTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        { yield return new Trajectory1(0.0, 400.0, 0.0, 2.0); } //large caliber bullet
    }

    public class InteriorTurret : Turret
    {
        public InteriorTurret(IMyLargeInteriorTurret _turret) : base(_turret) { }
        protected override IEnumerable<Trajectory1> Interceptor()
        { yield return new Trajectory1(0.0, 300.0, 0.0, 8.0 / 3.0); } //small caliber bullet
    }

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

        protected override Trajectory3 Carrier()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Trajectory1> Interceptor()
        {
            throw new NotImplementedException();
        }
    }

}
