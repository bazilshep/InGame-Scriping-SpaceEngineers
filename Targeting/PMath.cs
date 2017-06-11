using System;
using VRageMath; // VRage.Math.dll
using SpaceEngineersIngameScript.Util;

namespace SpaceEngineersIngameScript.Targeting
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

}
