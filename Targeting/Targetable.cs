using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.Targeting
{

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
    
}
