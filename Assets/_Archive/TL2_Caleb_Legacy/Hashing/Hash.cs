public static class Hash
{
    public static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    public static ulong Combine(ulong seed, int x, int y, uint salt)
    {
        unchecked
        {
            ulong h = Mix(seed);
            h = Mix(h ^ salt);
            h = Mix(h ^ (uint)x);
            h = Mix(h ^ (uint)y);
            return h;
        }
    }
}