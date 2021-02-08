using Sandbox.ModAPI.Ingame; // Sandbox.Common.dll
using System.Collections;
using System.Collections.Generic;

namespace IngameScript.TargetTracking
{

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

}
