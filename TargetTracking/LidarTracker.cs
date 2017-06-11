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

namespace SpaceEngineersIngameScript.TargetTracking
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
}
