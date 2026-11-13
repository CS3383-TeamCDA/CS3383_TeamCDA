using Unity.Profiling;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Counts GC allocation events inside an explicit main-thread scope; GC.Alloc values are time, not bytes.</summary>
    public sealed class CoordinateAllocationProbe : System.IDisposable
    {
        const int Capacity = 16384;
        ProfilerRecorder _recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", Capacity,
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
        public bool IsValid { get { return _recorder.Valid; } }
        public bool Overflowed { get; private set; }
        public void Begin() { _recorder.Stop(); _recorder.Reset(); _recorder.Start(); }
        public long End()
        {
            _recorder.Stop();
            Overflowed |= _recorder.Count >= Capacity;
            return _recorder.Count;
        }
        public void Dispose() { _recorder.Dispose(); }
    }
}
