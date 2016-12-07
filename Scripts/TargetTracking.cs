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

    /// <summary>
    /// Implements a target tracking algorithm for maintining multiple target locks.
    /// </summary>
    public class LidarTracker
    {
        public Action<string> logdelegate;
        void Log(string msg)
        {
            logdelegate?.Invoke(msg);
        }

        public LidarTracker(IMyCubeGrid mygrid)
        {
            Entities = new TrackedEntityCollection();
            pingqueue = new TargetPingQueue();
            pingHitMultiplier = new Dictionary<long, Ratio>();
            MyGrid = mygrid;
        }

        public TrackedEntityCollection Entities;

        public IMyCubeGrid MyGrid;

        const double Overscan = 20.0;
        const double LOST_TARGET_THRESHOLD = .01;

        List<IMyCameraBlock> Lidars;

        public TargetPingQueue pingqueue;

        #region "ping hit multiplier"                 
        Dictionary<long, Ratio> pingHitMultiplier; //Observed probability of ping hit, for learning                 

        void updatePingHitMultiplier(long id, bool pinghit)
        {
            Ratio prevRatio = getPingHitMultiplier(id);
            pingHitMultiplier[id] = new Ratio(prevRatio.num + (pinghit ? 1 : 0), prevRatio.den + 1);
        }

        Ratio getPingHitMultiplier(long id)
        {
            if (!pingHitMultiplier.ContainsKey(id)) { pingHitMultiplier[id] = new Ratio(2, 3); }
            return pingHitMultiplier[id];
        }

        void clearPingHitMultiplier(long id) { if (!pingHitMultiplier.ContainsKey(id)) { pingHitMultiplier.Remove(id); } }
        #endregion

        long CalculatePlannedPing(TrackedEntity e)
        {
            Vector3D size = e.Bound.Size;
            double surfaceArea = size.X * size.Y * size.Z / size.AbsMin();
            double dt = Math.Pow(1.3 * surfaceArea / (e.AccelSTD * e.AccelSTD + 1), .25);
            double adjust = getPingHitMultiplier(e.ID);
            dt = Math.Min(dt * adjust, 3.0);
            Log("Planning " + e.ToString() + " in " + ((e.LastTime - Now()) / 1000.0 + dt).ToString("0.000"));
            return e.LastTime + (long)(dt * 1000.0);
        }

        double PingProbability(TrackedEntity e)
        {
            return getPingHitMultiplier(e.ID) * e.HitProbability(Now());
        }

        public void PingPlannedTargets()
        {
            //Log("Ping in: " + ((pingqueue.Count > 0)?((pingqueue.peekFirst() -Now())/1000.0).ToString():"never"));        
            Dictionary<TrackedEntity, long> newplannedpings = new Dictionary<TrackedEntity, long>();
            while (pingqueue.Count > 0 && Now() > pingqueue.peekFirst())
            {
                TrackedEntity target = pingqueue.pop().Value;
                Log("Pinging " + target.ToString() + "   --s: " + target.Search.ToString());
                if (target.Search < 100)
                {
                    //result is bool pair (can_ping_target_position, did_hit_target)                  
                    MyTuple<bool, bool> result = PingEntity(target);
                    if (result.Item1)
                    {
                        if (result.Item2)
                        {
                            Log("Target Pinged");
                        }
                        else { Log("Target Missed"); }
                    }
                    else { Log("Target Unreachable: " + target.SearchPosition(Now(), MyGrid.GetPosition()).ToString("0.0")); }

                    if (!newplannedpings.ContainsKey(target) | target.DontTrack)
                    {
                        long plan = CalculatePlannedPing(target);
                        if (plan < Now()) { plan = Now(); }
                        newplannedpings.Add(target, plan);
                    }
                }
                else { Log("Target lost"); clearPingHitMultiplier(target.ID); }
            }
            foreach (var p in newplannedpings) if (p.Key.ID != MyGrid.EntityId) pingqueue.push(p.Value, p.Key);
        }

        public void UpdateWithExternalObservation(MyDetectedEntityInfo observation)
        {
            //workaround bugged position feedback on characters           
            if (((observation.Type == MyDetectedEntityType.CharacterHuman) & !observation.HitPosition.HasValue) |
                observation.EntityId == MyGrid.EntityId) { return; }

            UpdateTimeFromRadarPing(observation);
            TrackedEntity e = Entities.Update(ref observation);
            if (e != null && e.Type != MyDetectedEntityType.Unknown) { pingqueue.push(CalculatePlannedPing(e), e); }
        }

        // returns bool pair (can_ping_target_position, did-hit-target)                 
        MyTuple<bool, bool> PingEntity(TrackedEntity e)
        {
            Vector3D targetPosition = e.SearchPosition(Now(), MyGrid.GetPosition());
            MyTuple<bool, TrackedEntity> result = PingPosition(targetPosition);
            bool hitIntendedTarget = result.Item2 == e;
            bool canPingTargetPosition = result.Item1;
            if (canPingTargetPosition)
            {
                updatePingHitMultiplier(e.ID, hitIntendedTarget);
                if (!hitIntendedTarget) { e.LockedOn = false; e.Search++; }
            }
            return new MyTuple<bool, bool>(result.Item1, hitIntendedTarget);
        }

        // returns pair (can_ping_target_position, pinged_target)                 
        public MyTuple<bool, TrackedEntity> PingPosition(Vector3D targetPosition)
        {
            //Log("Pinging: " + targetPosition.ToString("0.0"));          
            KeyValuePair<long, IMyCameraBlock>? lidarsearchresult = pool.getFromLidarPool(targetPosition);
            if (lidarsearchresult.HasValue)
            {
                IMyCameraBlock lidar = lidarsearchresult.Value.Value;
                //Log("Max distance: " + lidar.AvailableScanRange.ToString("0.0"));         
                Vector3D v = targetPosition - lidar.GetPosition();
                Log("Pinging: " + v.Length().ToString("0.0"));
                v.Normalize();
                MyDetectedEntityInfo obs = lidar.Raycast(targetPosition + v * 19.0);
                pool.addToLidarPool(lidar);
                if (!obs.IsEmpty())
                {
                    UpdateTimeFromRadarPing(obs);
                    MyTuple<bool, TrackedEntity> rtn = new MyTuple<bool, TrackedEntity>(true, Entities.Update(ref obs));
                    if (rtn.Item2 != null)
                    {
                        pingqueue.push(CalculatePlannedPing(rtn.Item2), rtn.Item2);
                    }
                    Log("Pinged " + rtn.Item2.ToString());
                    return rtn;
                }
                else
                {
                    Log("Ping Missed");
                }
            }
            else
            {
                Log("Position Unreachable");
            }
            return new MyTuple<bool, TrackedEntity>(lidarsearchresult.HasValue, null);
        }

        LidarPool pool;
        public void Init_Sensors(List<IMyCameraBlock> sensors)
        {
            Lidars = new List<IMyCameraBlock>(sensors.Count);
            Lidars.AddRange(sensors);
            enableLidars();

            pool = new LidarPool(Now);
            foreach (IMyCameraBlock c in Lidars)
            {
                pool.addToLidarPool(c);
            }
        }
        void enableLidars()
        {
            for (int i = 0; i < Lidars.Count; i++)
            {
                Lidars[i].ApplyAction("OnOff_On");
                Lidars[i].EnableRaycast = true;
            }
        }

        #region "Time Now"                 

        private long _est_current_time = 0;
        private long time_update_iter = -1;
        public long Now() { return _est_current_time; }
        public void UpdateTimeFromProgramTick(IMyGridProgramRuntimeInfo info, long iter)
        {
            if (iter != time_update_iter)
            {
                time_update_iter = iter;
                _est_current_time += (long)info.TimeSinceLastRun.TotalMilliseconds;
            }
        }
        public void UpdateTimeFromRadarPing(MyDetectedEntityInfo obs)
        {
            if (obs.TimeStamp != 0) { _est_current_time = obs.TimeStamp; }
        }
        #endregion

    }

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

    /// <summary>
    /// Class for maintaining a queue of targets which need to be pinged.
    /// </summary>
    public class TargetPingQueue
    {
        public TargetPingQueue()
        {
            pingQueue = new SortedList<System.Tuple<long, long>, TrackedEntity>();
            EntityPingTime = new Dictionary<TrackedEntity, System.Tuple<long, long>>();
        }

        public int Count { get { return pingQueue.Count; } }

        SortedList<System.Tuple<long, long>, TrackedEntity> pingQueue; //Sorted list of planned target pings                 
        Dictionary<TrackedEntity, System.Tuple<long, long>> EntityPingTime;

        public long peekFirst() { return pingQueue.Keys[0].Item1; }

        public KeyValuePair<long, TrackedEntity> pop()
        {
            KeyValuePair<long, TrackedEntity> p = new KeyValuePair<long, TrackedEntity>(pingQueue.Keys[0].Item1, pingQueue.Values[0]);
            pingQueue.RemoveAt(0);
            EntityPingTime.Remove(p.Value);
            return p;
        }

        long pushCounter = 0; //monatonic counter for sorting oldest first if the timestamp is equal                 
        public void push(long t, TrackedEntity e)
        {
            if (EntityPingTime.ContainsKey(e))
            {
                pingQueue.Remove(EntityPingTime[e]);
                EntityPingTime.Remove(e);
            }
            pingQueue.Add(new System.Tuple<long, long>(t, pushCounter), e);
            EntityPingTime.Add(e, new System.Tuple<long, long>(t, pushCounter));
            pushCounter++;
        }
    }

    /// <summary>
    /// Class for storing a database of TrackedEntities and collect observations to update the targets.
    /// </summary>
    public class TrackedEntityCollection : IEnumerable<TrackedEntity>
    {
        public TrackedEntityCollection()
        {
            allEntities = new Dictionary<long, TrackedEntity>();
        }
        Dictionary<long, TrackedEntity> allEntities;

        void remove(long ID)
        {
            if (allEntities.ContainsKey(ID))
                allEntities.Remove(ID);
        }

        void add(TrackedEntity e)
        {
            allEntities[e.ID] = e;
        }

        public TrackedEntity Update(ref MyDetectedEntityInfo observation)
        {
            if (!observation.IsEmpty())
            {
                if (allEntities.ContainsKey(observation.EntityId))
                {
                    allEntities[observation.EntityId].Update(ref observation);
                }
                else
                {
                    add(new TrackedEntity(ref observation));
                }
                return allEntities[observation.EntityId];
            }
            else { return null; }
        }

        public void TrimEntities(long cutoff) //absolute cuttof time in ms                 
        {
            List<long> rem = new List<long>();
            foreach (TrackedEntity e in allEntities.Values)
            {
                if (e.Type != MyDetectedEntityType.Planet &
                    e.Type != MyDetectedEntityType.Asteroid &
                    e.LastTime < cutoff)
                {
                    rem.Add(e.ID);
                }
            }
            foreach (long e in rem) { remove(e); }
        }

        public TrackedEntity this[long ID] { get { if (allEntities.ContainsKey(ID)) return allEntities[ID]; else return null; } }

        Dictionary<long, TrackedEntity>.ValueCollection AllEntities() { return allEntities.Values; }

        public IEnumerator<TrackedEntity> GetEnumerator() { return allEntities.Values.GetEnumerator(); }

        public int Count()
        {
            return allEntities.Count;
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

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

    public struct Ratio
    {
        public Ratio(long n, long d) { num = n; den = d; }
        public readonly long num;
        public readonly long den;
        public static implicit operator Double(Ratio x)
        {
            return (double)x.num / x.den;
        }
    }

}
