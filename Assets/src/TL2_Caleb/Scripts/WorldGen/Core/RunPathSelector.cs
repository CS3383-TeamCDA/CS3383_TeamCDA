using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Deterministically chooses the next surface street for a forward run.</summary>
    public static class RunPathSelector
    {
        /// <summary>
        /// Chooses from connected streets, avoiding an immediate return and preferring movement away from
        /// the run origin. Recently used street IDs are avoided when a valid alternative exists.
        /// </summary>
        public static bool TryChooseNextStreet(WorldNetwork world, Int2 currentNode, Int2 previousNode,
            Int2 runOrigin, ulong runSeed, int stepIndex, StableId[] recentStreetIds,
            out IntersectionExit selected)
        {
            selected = default(IntersectionExit);
            if (world == null || stepIndex < 0) return false;
            IntersectionExit[] candidates = GetCandidates(world, currentNode, previousNode, runOrigin, recentStreetIds);
            if (candidates.Length == 0) return false;

            // GetIntersection returns exits in stable order, so the same run seed and step always pick the same route.
            StableId choiceId = StableId.ForStreet(runOrigin, currentNode);
            DeterministicRng rng = DeterministicRng.Create(runSeed, choiceId, stepIndex, Salt.RunRouteChoice);
            selected = candidates[rng.Next(candidates.Length)];
            return true;
        }

        /// <summary>Returns forward-run choices after backtrack, subway, outward-bias, and recent-route rules.</summary>
        public static IntersectionExit[] GetCandidates(WorldNetwork world, Int2 currentNode,
            Int2 previousNode, Int2 runOrigin, StableId[] recentStreetIds)
        {
            if (world == null) return new IntersectionExit[0];
            IntersectionExit[] exits = world.GetIntersection(currentNode);
            var candidates = new List<IntersectionExit>(exits.Length);
            var outward = new List<IntersectionExit>(exits.Length);
            long currentDistance = ManhattanDistance(currentNode, runOrigin);

            for (int i = 0; i < exits.Length; i++)
            {
                IntersectionExit exit = exits[i];
                if (exit.IsSubway || exit.Destination.Equals(previousNode)) continue;
                WorldStreet street = world.GetStreet(currentNode, exit.Destination);
                if (street == null || street.Layout == null) continue;
                candidates.Add(exit);
                if (ManhattanDistance(exit.Destination, runOrigin) > currentDistance)
                    outward.Add(exit);
            }

            if (outward.Count > 0) candidates = outward;
            var fresh = new List<IntersectionExit>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                StableId id = StableId.ForStreet(currentNode, candidates[i].Destination);
                if (!Contains(recentStreetIds, id)) fresh.Add(candidates[i]);
            }
            return (fresh.Count > 0 ? fresh : candidates).ToArray();
        }

        static bool Contains(StableId[] values, StableId value)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++)
                if (values[i].Equals(value)) return true;
            return false;
        }

        static long ManhattanDistance(Int2 a, Int2 b)
        {
            return Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y);
        }
    }
}
