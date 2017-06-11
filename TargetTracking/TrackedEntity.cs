using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System;
using VRage.Game; // VRage.Game.dll
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.TargetTracking
{

    /// <summary>
    /// Class for storing information about a tracked entity.
    /// </summary>
    public class TrackedEntity
    {
        //exponential decay filter factor for acceleration                 
        const double AccelFilter = 0.1237; //Math.Exp(2*PI*Period) with Period=3s                 

        public long ID;
        public string Name;
        public long LastTime;
        public Vector3D Position;
        public Vector3D Velocity;
        public Vector3D Acceleration;
        public double AccelSTD; //average deviation of acceleration from average                 
        public MatrixD Orientation;
        public BoundingBoxD Bound;
        public MyDetectedEntityType Type;
        public MyRelationsBetweenPlayerAndBlock Relationship;
        public bool LockedOn = false;
        public int Search = 0;
        public bool DontTrack = false;

        public void Update(ref MyDetectedEntityInfo obs)
        {
            Name = obs.Name;
            Position = obs.Position;
            if (LastTime != obs.TimeStamp) // dont update this multiple times in one tick                 
            {
                Vector3D obsAccel = (obs.Velocity - Velocity) / (obs.TimeStamp - LastTime);
                double dt = .001 * (obs.TimeStamp - LastTime);
                double filt = Math.Pow(AccelFilter, dt);
                double ferr = Math.Pow(.9, dt);
                double AccelErr = (Acceleration - obsAccel).Length() / dt;
                AccelSTD = Math.Max(AccelErr, AccelErr * (1.0 - ferr) + AccelSTD * ferr); //update acceleration variance estimate                 
                Acceleration = obsAccel * (1.0 - filt) + Acceleration * filt; //update Low pass filtered acceleration estimate                 
            }
            Velocity = obs.Velocity;
            Bound = obs.BoundingBox;
            Orientation = obs.Orientation;
            LastTime = obs.TimeStamp;
            Relationship = obs.Relationship;
            LockedOn = true;
            Search = 0;
        }

        public TrackedEntity(ref MyDetectedEntityInfo obs)
        {
            ID = obs.EntityId;
            Name = obs.Name;
            Type = obs.Type;
            Position = obs.Position;
            Velocity = obs.Velocity;
            Acceleration = Vector3D.Zero;
            AccelSTD = 15;//initialize to high uncertainty (15m/s^2)                 
            Bound = obs.BoundingBox;
            Orientation = obs.Orientation;
            LastTime = obs.TimeStamp;
            Relationship = obs.Relationship;
        }

        public Vector3D PredictedPosition(long time)
        {
            double dt = .001 * (time - LastTime);
            return Position + dt * Velocity + .5 * dt * dt * Acceleration;
        }

        public Vector3D SearchPosition(long time, Vector3D trackerPosition)
        {
            Vector3D nominalPosition = PredictedPosition(time);
            if (Search == 0) { return nominalPosition; }

            Vector3D v = nominalPosition - trackerPosition;
            v.Normalize();
            Vector3D u = Vector3D.Cross(v, Velocity);
            if (u.Length() == 0) { u = Vector3D.CalculatePerpendicularVector(v); }
            u.Normalize();
            Vector3D w = Vector3D.Cross(v, u);
            w.Normalize();
            double l = Bound.Size.AbsMin() / 2;

            //search path is a spiral, where each ring is l apart, and each successive point is l apart.       
            double theta = Math.Sqrt(Math.PI * (2 * (Search - 1) + Math.PI)) - Math.PI;
            double r = l * (theta / 2 / Math.PI + 1);

            return nominalPosition + u * r * Math.Cos(theta) + w * r * Math.Sin(theta) + v * 2 * l;
        }

        public double ExpectedPredictionError(long time) { double dt = .001 * (time - LastTime); return .5 * dt * dt * AccelSTD; }

        public bool IsEnemy { get { return Relationship == MyRelationsBetweenPlayerAndBlock.Enemies; } }

        public double HitProbability(long t)
        {
            Vector3D size = Bound.Size;
            double surfaceArea = size.X * size.Y * size.Z / size.AbsMax();
            double dt = .001 * (t - LastTime);
            double reachableArea = .78 * (AccelSTD * AccelSTD) * dt * dt * dt * dt;
            double p = surfaceArea / reachableArea;
            return Math.Min(p, 1.0);
        }

        public override string ToString()
        {
            return (LockedOn ? "  -LOCK-  " : "SEARCH ") + Name + " - " + Type.ToString();
        }

    }

}
