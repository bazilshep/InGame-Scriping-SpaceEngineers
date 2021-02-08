using System.Collections.Generic;
using System;

namespace IngameScript.TargetTracking
{

    /// <summary>
    /// Class for maintaining a queue of targets which need to be pinged.
    /// </summary>
    public class TargetPingQueue
    {
        public TargetPingQueue()
        {
            pingQueue = new SortedList<ValueTuple<long, long>, TrackedEntity>();
            EntityPingTime = new Dictionary<TrackedEntity, ValueTuple<long, long>>();
        }

        public int Count { get { return pingQueue.Count; } }

        SortedList<ValueTuple<long, long>, TrackedEntity> pingQueue; //Sorted list of planned target pings                 
        Dictionary<TrackedEntity, ValueTuple<long, long>> EntityPingTime;

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
            pingQueue.Add(new ValueTuple<long, long>(t, pushCounter), e);
            EntityPingTime.Add(e, new ValueTuple<long, long>(t, pushCounter));
            pushCounter++;
        }
    }

}
