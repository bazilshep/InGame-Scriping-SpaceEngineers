using System.Collections.Generic;

namespace SpaceEngineersIngameScript.TargetTracking
{

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

}
