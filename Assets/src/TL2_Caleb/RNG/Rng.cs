public struct Rng
{
    const ulong Gamma = 0x9E3779B97F4A7C15UL;   // golden-ratio increment
    ulong state;

    public Rng(ulong seed)
    {
        state = seed;
    }

    // advances state, returns 64 random bits
    public ulong NextU64()
    {
        unchecked { state += Gamma; }
        return Hash.Mix(state);
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new System.ArgumentOutOfRangeException(nameof(maxExclusive), "max must be greater than min");

        ulong span = (ulong)((long)maxExclusive - minInclusive);   // long math so int.MinValue..int.MaxValue can't overflow
        return (int)(minInclusive + (long)(NextU64() % span));
    }
}