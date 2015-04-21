using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    public class Profiler : Gear
    {
        internal ProfilerWindow Window { get; private set; }

        readonly TimeRuler _timeRuler;
        readonly PerformanceGraph _performanceGraph;

        private bool locationJustChanged;

        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        /// <summary>
        /// Gets/Set log display or no.
        /// </summary>
        public bool ShowLog { get; set; }

        /// <summary>
        /// Gets/Sets target sample frames.
        /// </summary>
        public int TargetSampleFrames { get; set; }

        /// <summary>
        /// Gets/Sets TimeRuler rendering position.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets/Sets timer ruler width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Max bar count.
        /// </summary>
        const int MaxBars = 16;

        /// <summary>
        /// Maximum sample number for each bar.
        /// </summary>
        const int MaxSamples = 2560;

        /// <summary>
        /// Maximum nest calls for each bar.
        /// </summary>
        const int MaxNestCall = 32;

        /// <summary>Maximum display frames.</summary>
        const int MaxSampleFrames = 4;

        /// <summary>
        /// Duration (in frame count) for take snap shot of log.
        /// </summary>
        const int LogSnapDuration = 120;

        /// <summary>
        /// Marker structure.
        /// </summary>
        internal struct Marker
        {
            public int MarkerId;
            public float BeginTime;
            public float EndTime;
            public Color Color;
        }

        /// <summary>
        /// Collection of markers.
        /// </summary>
        internal class MarkerCollection
        {
            // Marker collection.
            public readonly Marker[] Markers = new Marker[MaxSamples];
            public int MarkCount;

            // Marker nest information.
            public readonly int[] MarkerNests = new int[MaxNestCall];
            public int NestCount;
        }

        /// <summary>
        /// Frame logging information.
        /// </summary>
        internal class FrameLog
        {
            public readonly MarkerCollection[] Bars;

            public FrameLog()
            {
                // Initialize markers.
                Bars = new MarkerCollection[MaxBars];
                for (var i = 0; i < MaxBars; ++i)
                    Bars[i] = new MarkerCollection();
            }
        }

        /// <summary>
        /// Marker information
        /// </summary>
        private class MarkerInfo
        {
            // Name of marker.
            public readonly string Name;

            // Marker log.
            public readonly MarkerLog[] Logs = new MarkerLog[MaxBars];

            public MarkerInfo(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        /// Marker log information.
        /// </summary>
        private struct MarkerLog
        {
            public float SnapAvg;

            public float Min;
            public float Max;
            public float Avg;

            public int Samples;

            public Color Color;

            public bool Initialized;
        }

        // Logs for each frames.
        readonly FrameLog[] _logs;

        // Previous frame log.
        FrameLog _prevLog;

        // Current log.
        FrameLog _curLog;

        // Current frame count.
        int _frameCount;

        // Stopwatch for measure the time.
        readonly Stopwatch _stopwatch = new Stopwatch();

        // Marker information array.
        readonly List<MarkerInfo> _markers = new List<MarkerInfo>();

        // Dictionary that maps from marker name to marker id.
        readonly Dictionary<string, int> _markerNameToIdMap = new Dictionary<string, int>();


        // You want to call StartFrame at beginning of Game.Update method.
        // But Game.Update gets calls multiple time when game runs slow in fixed time step mode.
        // In this case, we should ignore StartFrame call.
        // To do this, we just keep tracking of number of StartFrame calls until Draw gets called.
        int _updateCount;

        public Profiler() : base(GearsetSettings.Instance.ProfilerConfig)
        {            
            // Create the window.
            Window = new ProfilerWindow
            {
                Top = Config.Top,
                Left = Config.Left,
                Width = Config.Width,
                Height = Config.Height
            };

            Window.IsVisibleChanged += ProfilerIsVisibleChanged;

            WindowHelper.EnsureOnScreen(Window);

            if (Config.Visible)
                Window.Show();

            Window.LocationChanged += ProfilerLocationChanged;
            Window.SizeChanged += ProfilerSizeChanged;

            _logs = new FrameLog[2];
            for (var i = 0; i < _logs.Length; ++i)
                _logs[i] = new FrameLog();

            _timeRuler = new TimeRuler(TargetSampleFrames = 1, Vector2.Zero, new Vector2(GearsetResources.Device.Viewport.Width, 10));
            _performanceGraph = new PerformanceGraph(TargetSampleFrames = 1, new Vector2(0, 11), new Vector2(GearsetResources.Device.Viewport.Width, 10000/60.0f ));
        }

        void ProfilerIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Config.Visible = Window.IsVisible;
        }

        protected override void OnVisibleChanged()
        {
            if (Window != null)
                Window.Visibility = Visible ? Visibility.Visible : Visibility.Hidden;
        }

        void ProfilerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            locationJustChanged = true;
        }

        void ProfilerLocationChanged(object sender, EventArgs e)
        {
            locationJustChanged = true;
        }

        /// <summary>
        /// Start new frame.
        /// </summary>
        public void StartFrame()
        {
            lock (this)
            {
                // We skip reset frame when this method gets called multiple times.
                var count = Interlocked.Increment(ref _updateCount);
                if (Visible && (1 < count && count < MaxSampleFrames))
                    return;

                // Update current frame log.
                _prevLog = _logs[_frameCount++ & 0x1];
                _curLog = _logs[_frameCount & 0x1];

                var endFrameTime = (float)_stopwatch.Elapsed.TotalMilliseconds;

                // Update marker and create a log.
                for (var barIdx = 0; barIdx < _prevLog.Bars.Length; ++barIdx)
                {
                    var prevBar = _prevLog.Bars[barIdx];
                    var nextBar = _curLog.Bars[barIdx];

                    // Re-open marker that didn't get called EndMark in previous frame.
                    for (var nest = 0; nest < prevBar.NestCount; ++nest)
                    {
                        var markerIdx = prevBar.MarkerNests[nest];

                        prevBar.Markers[markerIdx].EndTime = endFrameTime;

                        nextBar.MarkerNests[nest] = nest;
                        nextBar.Markers[nest].MarkerId = prevBar.Markers[markerIdx].MarkerId;
                        nextBar.Markers[nest].BeginTime = 0;
                        nextBar.Markers[nest].EndTime = -1;
                        nextBar.Markers[nest].Color = prevBar.Markers[markerIdx].Color;
                    }

                    // Update marker log.
                    for (var markerIdx = 0; markerIdx < prevBar.MarkCount; ++markerIdx)
                    {
                        var duration = prevBar.Markers[markerIdx].EndTime - prevBar.Markers[markerIdx].BeginTime;
                        var markerId = prevBar.Markers[markerIdx].MarkerId;
                        var m = _markers[markerId];

                        m.Logs[barIdx].Color = prevBar.Markers[markerIdx].Color;

                        if (!m.Logs[barIdx].Initialized)
                        {
                            // First frame process.
                            m.Logs[barIdx].Min = duration;
                            m.Logs[barIdx].Max = duration;
                            m.Logs[barIdx].Avg = duration;

                            m.Logs[barIdx].Initialized = true;
                        }
                        else
                        {
                            // Process after first frame.
                            m.Logs[barIdx].Min = Math.Min(m.Logs[barIdx].Min, duration);
                            m.Logs[barIdx].Max = Math.Min(m.Logs[barIdx].Max, duration);
                            m.Logs[barIdx].Avg += duration;
                            m.Logs[barIdx].Avg *= 0.5f;

                            if (m.Logs[barIdx].Samples++ >= LogSnapDuration)
                            {
                                //m.Logs[barIdx].SnapMin = m.Logs[barIdx].Min;
                                //m.Logs[barIdx].SnapMax = m.Logs[barIdx].Max;
                                m.Logs[barIdx].SnapAvg = m.Logs[barIdx].Avg;
                                m.Logs[barIdx].Samples = 0;
                            }
                        }
                    }

                    nextBar.MarkCount = prevBar.NestCount;
                    nextBar.NestCount = prevBar.NestCount;
                }

                // Start measuring.
                _stopwatch.Reset();
                _stopwatch.Start();
            }
        }

        /// <summary>
        /// Start measure time.
        /// </summary>
        /// <param name="markerName">name of marker.</param>
        /// <param name="color">color</param>
        public void BeginMark(string markerName, Color color)
        {
            BeginMark(0, markerName, color);
        }

        /// <summary>
        /// Start measure time.
        /// </summary>
        /// <param name="barIndex">index of bar</param>
        /// <param name="markerName">name of marker.</param>
        /// <param name="color">color</param>
        public void BeginMark(int barIndex, string markerName, Color color)
        {
            lock (this)
            {
                if (barIndex < 0 || barIndex >= MaxBars)
                    throw new ArgumentOutOfRangeException("barIndex");

                var bar = _curLog.Bars[barIndex];

                if (bar.MarkCount >= MaxSamples)
                {
                    throw new OverflowException("Exceeded sample count.\n" + "Either set larger number to TimeRuler.MaxSmpale or" + "lower sample count.");
                }

                if (bar.NestCount >= MaxNestCall)
                {
                    throw new OverflowException("Exceeded nest count.\n" + "Either set larget number to TimeRuler.MaxNestCall or" + "lower nest calls.");
                }

                // Gets registered marker.
                int markerId;
                if (!_markerNameToIdMap.TryGetValue(markerName, out markerId))
                {
                    // Register this if this marker is not registered.
                    markerId = _markers.Count;
                    _markerNameToIdMap.Add(markerName, markerId);
                    _markers.Add(new MarkerInfo(markerName));
                }

                // Start measuring.
                bar.MarkerNests[bar.NestCount++] = bar.MarkCount;

                // Fill marker parameters.
                bar.Markers[bar.MarkCount].MarkerId = markerId;
                bar.Markers[bar.MarkCount].Color = color;
                bar.Markers[bar.MarkCount].BeginTime = (float)_stopwatch.Elapsed.TotalMilliseconds;

                bar.Markers[bar.MarkCount].EndTime = -1;

                bar.MarkCount++;
            }
        }

        /// <summary>
        /// End measuring.
        /// </summary>
        /// <param name="markerName">Name of marker.</param>
        public void EndMark(string markerName)
        {
            EndMark(0, markerName);
        }

        /// <summary>
        /// End measuring.
        /// </summary>
        /// <param name="barIndex">Index of bar.</param>
        /// <param name="markerName">Name of marker.</param>
        public void EndMark(int barIndex, string markerName)
        {
            lock (this)
            {
                if (barIndex < 0 || barIndex >= MaxBars)
                    throw new ArgumentOutOfRangeException("barIndex");

                var bar = _curLog.Bars[barIndex];

                if (bar.NestCount <= 0)
                    throw new InvalidOperationException("Call BeingMark method before call EndMark method.");

                int markerId;
                if (!_markerNameToIdMap.TryGetValue(markerName, out markerId))
                    throw new InvalidOperationException(String.Format("Maker '{0}' is not registered." + "Make sure you specifed same name as you used for BeginMark" + " method.", markerName));

                var markerIdx = bar.MarkerNests[--bar.NestCount];
                if (bar.Markers[markerIdx].MarkerId != markerId)
                {
                    throw new InvalidOperationException("Incorrect call order of BeginMark/EndMark method." + "You call it like BeginMark(A), BeginMark(B), EndMark(B), EndMark(A)" +
                            " But you can't call it like " + "BeginMark(A), BeginMark(B), EndMark(A), EndMark(B).");
                }

                bar.Markers[markerIdx].EndTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Get average time of given bar index and marker name.
        /// </summary>
        /// <param name="barIndex">Index of bar</param>
        /// <param name="markerName">name of marker</param>
        /// <returns>average spending time in ms.</returns>
        public float GetAverageTime(int barIndex, string markerName)
        {
            if (barIndex < 0 || barIndex >= MaxBars)
                throw new ArgumentOutOfRangeException("barIndex");

            float result = 0;
            int markerId;
            if (_markerNameToIdMap.TryGetValue(markerName, out markerId))
                result = _markers[markerId].Logs[barIndex].Avg;

            return result;
        }

        /// <summary>
        /// Reset marker log.
        /// </summary>
        public void ResetLog()
        {
            lock (this)
            {
                foreach (var markerInfo in _markers)
                {
                    for (var i = 0; i < markerInfo.Logs.Length; ++i)
                    {
                        markerInfo.Logs[i].Initialized = false;
                        markerInfo.Logs[i].SnapAvg = 0;

                        markerInfo.Logs[i].Min = 0;
                        markerInfo.Logs[i].Max = 0;
                        markerInfo.Logs[i].Avg = 0;

                        markerInfo.Logs[i].Samples = 0;
                    }
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (locationJustChanged)
            {
                locationJustChanged = false;
                Config.Top = Window.Top;
                Config.Left = Window.Left;
                Config.Width = Window.Width;
                Config.Height = Window.Height;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Just to make sure we're only doing this one per frame.
            if (GearsetResources.CurrentRenderPass != RenderPass.BasicEffectPass)
                return;

            // Reset update count.
            Interlocked.Exchange(ref _updateCount, 0);

            _timeRuler.Draw(_prevLog);
            _performanceGraph.Draw(_prevLog);
        }
    }
}
