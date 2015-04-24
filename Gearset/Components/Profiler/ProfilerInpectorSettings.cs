namespace Gearset.Components.Profiler
{
    /// <summary>
    /// Acts as a simplified wrapper for configurig the profiler (at runtime)
    /// </summary>
    public class ProfilerInpectorSettings
    {
        readonly Profiler _profiler;       

        public ProfilerInpectorSettings(Profiler profiler)
        {
            _profiler = profiler;
        }

        public bool Sleep
        {
            get { return _profiler.Sleep; }
            set { _profiler.Sleep = value; }
        }

        public bool SkipUpdate
        {
            get { return _profiler.SkipUpdate; }
            set { _profiler.SkipUpdate = value; }
        }

        public uint MaxFrameCount
        {
            get { return PerformanceGraph.MaxFrames; }
        }

        public uint DisplayedFrameCount
        {
            get { return _profiler.PerformanceGraph.DisplayedFrameCount; }
            set { _profiler.PerformanceGraph.DisplayedFrameCount = value; }
        }

        public uint SkipFrames
        {
            get { return _profiler.PerformanceGraph.SkipFrames; }
            set { _profiler.PerformanceGraph.SkipFrames = value; }
        }
    }
}
