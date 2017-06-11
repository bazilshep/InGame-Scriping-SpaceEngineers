using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System;
using System.Collections.Generic;
using VRageMath; // VRage.Math.dll

namespace SpaceEngineersIngameScript.TargetTracking
{

    /// <summary>
    /// Class for managing a collection of IMyCameraBlock objects.
    /// </summary>
    public class LidarPool
    {
        //monatonic counter for sorting oldest first if the timestamp is equal                 
        long addedCount = 0;

        //sorted by (extrapolated) timestamp last depleted, so that the least utilized is first                 
        SortedList<System.Tuple<long, long>, IMyCameraBlock> LidarQueue;
        Func<long> currentTimeGetter;

        long lidarDepletedTimeStamp(IMyCameraBlock c)
        {
            //charge time per distance                 
            double chargerate = c.TimeUntilScan(c.AvailableScanRange + 1.0);
            return currentTimeGetter() - (long)(c.AvailableScanRange * chargerate);
        }

        public LidarPool(Func<long> getcurrenttime)
        {
            currentTimeGetter = getcurrenttime;
            LidarQueue = new SortedList<System.Tuple<long, long>, IMyCameraBlock>();
        }

        public static bool LidarCanPing(IMyCameraBlock c, Vector3D target)
        {
            double maxdot = Math.Sin(c.RaycastConeLimit * Math.PI / 180.0);

            Vector3D v = target - c.GetPosition();
            v.Normalize();

            Vector3D tc = Vector3.TransformNormal(v, Matrix.Transpose(c.WorldMatrix));

            target = target + v * 20.0;

            return tc.Z < 0 && Math.Abs(tc.Y) < maxdot && Math.Abs(tc.X) < maxdot && ((target - c.WorldMatrix.Translation).Length() < c.AvailableScanRange);
        }

        //gets a lidar from the pool which can ping the target point                 
        //all lidars should be returned using addToLidarPool after use                 
        public KeyValuePair<long, IMyCameraBlock>? getFromLidarPool(Vector3D target)
        {
            KeyValuePair<System.Tuple<long, long>, IMyCameraBlock>? found = null;
            foreach (KeyValuePair<System.Tuple<long, long>, IMyCameraBlock> pair in LidarQueue)
            {
                if (pair.Value.IsWorking)
                    if (LidarCanPing(pair.Value, target)) { found = pair; break; };
            }

            if (found.HasValue)
            {
                LidarQueue.Remove(found.Value.Key); //remove it from pool                 
                                                    //return the pair (timestampdepleted,lidar)                 
                KeyValuePair<long, IMyCameraBlock> rtn = new KeyValuePair<long, IMyCameraBlock>(found.Value.Key.Item1, found.Value.Value);
                return rtn;
            }
            return null;
        }

        public long addToLidarPool(IMyCameraBlock c)
        {
            long timestamp = lidarDepletedTimeStamp(c);
            LidarQueue.Add(new System.Tuple<long, long>(timestamp, addedCount++), c);
            return timestamp;
        }
    }

}
