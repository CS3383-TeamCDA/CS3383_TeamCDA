// Stateless hashing. Everything in world generation derives its randomness from
// hash(seed, stable ids...) so results never depend on the order things are generated in.
namespace EscapeThe90s.WorldGen
{
    public static class WorldHash
    {
        const ulong Gamma = 0x9E3779B97F4A7C15UL;

        // SplitMix64 finalizer: good avalanche, fully integer, identical on every platform.
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z += Gamma;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        // Order-dependent combine: Combine(Combine(h, a), b) != Combine(Combine(h, b), a).
        public static ulong Combine(ulong h, ulong v)
        {
            unchecked { return Mix(h ^ (Mix(v) + Gamma + (h << 6) + (h >> 2))); }
        }

        public static ulong Combine(ulong h, int v) { unchecked { return Combine(h, (ulong)(long)v); } }
    }
}
