namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Small value-type PRNG (SplitMix64 stream). Create one per entity via the Create methods;
    /// never share a single instance across unrelated generation steps.
    /// Do NOT use UnityEngine.Random for world generation: it is global state.
    /// </summary>
    public struct DeterministicRng
    {
        ulong _state;
        const ulong Gamma = 0x9E3779B97F4A7C15UL;

        public DeterministicRng(ulong state) { _state = state; }

        public static DeterministicRng Create(ulong seed, StableId id, int salt)
        {
            ulong h = WorldHash.Combine(seed, id.Value);
            return new DeterministicRng(WorldHash.Combine(h, salt));
        }

        public static DeterministicRng Create(ulong seed, StableId id, int index, int salt)
        {
            ulong h = WorldHash.Combine(seed, id.Value);
            h = WorldHash.Combine(h, index);
            return new DeterministicRng(WorldHash.Combine(h, salt));
        }

        public ulong NextULong()
        {
            unchecked
            {
                _state += Gamma;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public uint NextUInt() { return (uint)(NextULong() >> 32); }

        /// <summary>Uniform in [0, maxExclusive). Returns 0 if maxExclusive &lt;= 1.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>Uniform in [min, maxExclusive).</summary>
        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + Next(maxExclusive - min);
        }

        /// <summary>True with probability percent/100. Integer math only.</summary>
        public bool ChancePercent(int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            return Next(100) < percent;
        }

        /// <summary>Index chosen proportionally to integer weights, or -1 if no weight is positive.</summary>
        public int PickWeighted(int[] weights)
        {
            long total = 0;
            for (int i = 0; i < weights.Length; i++) if (weights[i] > 0) total += weights[i];
            if (total <= 0) return -1;
            long roll = (long)(NextULong() % (ulong)total);
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0) continue;
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return -1;
        }
    }
}
