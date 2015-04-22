using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;

namespace Gearset.Components.Profiler
{
    public class Profiler : Gear
    {
        public class LevelItem : IComparable<LevelItem>, INotifyPropertyChanged
        {
            public String Name { get; set; }
            public Boolean Enabled { get { return _enabled; } set { _enabled = value; OnPropertyChanged("Enabled"); } }

            public Brush Color { get; set; }

            private void OnPropertyChanged(string p)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
            private Boolean _enabled = true;
            public event PropertyChangedEventHandler PropertyChanged;

            public int CompareTo(LevelItem other)
            {
                return String.Compare(Name, other.Name, true, CultureInfo.InvariantCulture);
            }
        }

        //The number of 
        public const int MaxLevels = 8;

        internal ProfilerWindow Window { get; private set; }

        public TimeRuler TimeRuler { get; private set; }
        public PerformanceGraph PerformanceGraph { get; private set; }

        private bool _locationJustChanged;

        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        //Settings the game can use for Profiling scenarios (e.g. to test if CPU/GPU bound)
        public bool Sleep { get; set; }
        public bool SkipUpdate { get; set; }

        /// <summary>
        /// Gets/Sets target sample frames.
        /// </summary>
        public int TargetSampleFrames { get; set; }        

        /// <summary>
        /// Maximum sample number for each level.
        /// </summary>
        const int MaxSamples = 2560;

        /// <summary>
        /// Maximum nest calls for each level.
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
            public readonly MarkerCollection[] Levels;

            public FrameLog()
            {
                // Initialize markers.
                Levels = new MarkerCollection[MaxLevels];
                for (var i = 0; i < MaxLevels; ++i)
                    Levels[i] = new MarkerCollection();
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
            public readonly MarkerLog[] Logs = new MarkerLog[MaxLevels];

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

        readonly InternalLabeler _internalLabeler = new InternalLabeler();

        // You want to call StartFrame at beginning of Game.Update method.
        // But Game.Update gets calls multiple time when game runs slow in fixed time step mode.
        // In this case, we should ignore StartFrame call.
        // To do this, we just keep tracking of number of StartFrame calls until Draw gets called.
        int _updateCount;

        public Profiler() : base(GearsetSettings.Instance.ProfilerConfig)
        {
            CreateProfilerWindow();

            Children.Add(_internalLabeler);

            _logs = new FrameLog[2];
            for (var i = 0; i < _logs.Length; ++i)
                _logs[i] = new FrameLog();

            CreateTimeRuler();
            CreatePerformanceGraph();
        }

        void CreateProfilerWindow()
        {
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

            WpfTest();
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        internal ObservableCollection<LevelItem> Levels;

        private readonly SolidColorBrush[] colors = { 
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 221, 221, 221)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 128, 200, 200)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 200, 128)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 128, 200)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 128, 128, 200)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 128, 200, 128)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 128, 128)),
            new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 150, 110, 110)),
        };

        void WpfTest()
        {
            Levels = new ObservableCollection<LevelItem>();

            for(var i = 0; i < MaxLevels; i++)
                Levels.Add(new LevelItem { Name = "Level " + i, Enabled = true, Color = colors[0] });

            Window.LevelsListBox.DataContext = Levels;
        }

        public void EnableAllLevels()
        {
            foreach (var level in Levels)
                level.Enabled = true;
        }

        public void DisableAllLevels()
        {
            foreach (var level in Levels)
                level.Enabled = false;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------

        void CreateTimeRuler()
        {
            TargetSampleFrames = 1;

            var minSize = new Vector2(100, 16);
            var size = Vector2.Max(minSize, Config.TimeRulerSize);

            TimeRuler = new TimeRuler(this, TargetSampleFrames, Config.TimeRulerPosition, size);

            TimeRuler.Visible = Config.TimeRulerVisible;

            TimeRuler.VisibleChanged += (sender, args) => { 
                Config.TimeRulerVisible = TimeRuler.Visible; 
            };
            
            TimeRuler.Dragged += (object sender, ref Vector2 args) => { 
                Config.TimeRulerPosition = TimeRuler.Position; 
            };
            
            TimeRuler.ScaleNob.Dragged += (object sender, ref Vector2 args) => { 
                Config.TimeRulerSize = TimeRuler.Size; 
            };
        }

        void CreatePerformanceGraph()
        {
            var minSize = new Vector2(100, 16);
            var size = Vector2.Max(minSize, Config.PerformanceGraphSize);

            PerformanceGraph = new PerformanceGraph(this, Config.PerformanceGraphPosition, size);

            PerformanceGraph.Visible = Config.PerformanceGraphVisible;

            PerformanceGraph.VisibleChanged += (sender, args) => {
                Config.PerformanceGraphVisible = PerformanceGraph.Visible;
            };

            PerformanceGraph.Dragged += (object sender, ref Vector2 args) =>
            {
                Config.PerformanceGraphPosition = PerformanceGraph.Position;
            };

            PerformanceGraph.ScaleNob.Dragged += (object sender, ref Vector2 args) =>
            {
                Config.PerformanceGraphSize = PerformanceGraph.Size;
            };
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
            _locationJustChanged = true;
        }

        void ProfilerLocationChanged(object sender, EventArgs e)
        {
            _locationJustChanged = true;
        }

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
                for (var levelIdx = 0; levelIdx < _prevLog.Levels.Length; ++levelIdx)
                {
                    var prevLevel = _prevLog.Levels[levelIdx];
                    var nextLevel = _curLog.Levels[levelIdx];

                    // Re-open marker that didn't get called EndMark in previous frame.
                    for (var nest = 0; nest < prevLevel.NestCount; ++nest)
                    {
                        var markerIdx = prevLevel.MarkerNests[nest];

                        prevLevel.Markers[markerIdx].EndTime = endFrameTime;

                        nextLevel.MarkerNests[nest] = nest;
                        nextLevel.Markers[nest].MarkerId = prevLevel.Markers[markerIdx].MarkerId;
                        nextLevel.Markers[nest].BeginTime = 0;
                        nextLevel.Markers[nest].EndTime = -1;
                        nextLevel.Markers[nest].Color = prevLevel.Markers[markerIdx].Color;
                    }

                    // Update marker log.
                    for (var markerIdx = 0; markerIdx < prevLevel.MarkCount; ++markerIdx)
                    {
                        var duration = prevLevel.Markers[markerIdx].EndTime - prevLevel.Markers[markerIdx].BeginTime;
                        var markerId = prevLevel.Markers[markerIdx].MarkerId;
                        var m = _markers[markerId];

                        m.Logs[levelIdx].Color = prevLevel.Markers[markerIdx].Color;

                        if (!m.Logs[levelIdx].Initialized)
                        {
                            // First frame process.
                            m.Logs[levelIdx].Min = duration;
                            m.Logs[levelIdx].Max = duration;
                            m.Logs[levelIdx].Avg = duration;

                            m.Logs[levelIdx].Initialized = true;
                        }
                        else
                        {
                            // Process after first frame.
                            m.Logs[levelIdx].Min = Math.Min(m.Logs[levelIdx].Min, duration);
                            m.Logs[levelIdx].Max = Math.Min(m.Logs[levelIdx].Max, duration);
                            m.Logs[levelIdx].Avg += duration;
                            m.Logs[levelIdx].Avg *= 0.5f;

                            if (m.Logs[levelIdx].Samples++ >= LogSnapDuration)
                            {
                                //m.Logs[levelIdx].SnapMin = m.Logs[levelIdx].Min;
                                //m.Logs[levelIdx].SnapMax = m.Logs[levelIdx].Max;
                                m.Logs[levelIdx].SnapAvg = m.Logs[levelIdx].Avg;
                                m.Logs[levelIdx].Samples = 0;
                            }
                        }
                    }

                    nextLevel.MarkCount = prevLevel.NestCount;
                    nextLevel.NestCount = prevLevel.NestCount;
                }

                // Start measuring.
                _stopwatch.Reset();
                _stopwatch.Start();
            }
        }

        public void BeginMark(string markerName, Color color)
        {
            BeginMark(0, markerName, color);
        }

        public void BeginMark(int levelIndex, string markerName, Color color)
        {
            lock (this)
            {
                if (levelIndex < 0 || levelIndex >= MaxLevels)
                    throw new ArgumentOutOfRangeException("levelIndex");

                var level = _curLog.Levels[levelIndex];

                if (level.MarkCount >= MaxSamples)
                    throw new OverflowException("Exceeded sample count.\n" + "Either set larger number to TimeRuler.MaxSmpale or" + "lower sample count.");

                if (level.NestCount >= MaxNestCall)
                    throw new OverflowException("Exceeded nest count.\n" + "Either set larget number to TimeRuler.MaxNestCall or" + "lower nest calls.");

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
                level.MarkerNests[level.NestCount++] = level.MarkCount;

                // Fill marker parameters.
                level.Markers[level.MarkCount].MarkerId = markerId;
                level.Markers[level.MarkCount].Color = color;
                level.Markers[level.MarkCount].BeginTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
                level.Markers[level.MarkCount].EndTime = -1;

                level.MarkCount++;
            }
        }

        public void EndMark(string markerName)
        {
            EndMark(0, markerName);
        }

        public void EndMark(int levelIndex, string markerName)
        {
            lock (this)
            {
                if (levelIndex < 0 || levelIndex >= MaxLevels)
                    throw new ArgumentOutOfRangeException("levelIndex");

                var level = _curLog.Levels[levelIndex];

                if (level.NestCount <= 0)
                    throw new InvalidOperationException("Call BeingMark method before call EndMark method.");

                int markerId;
                if (!_markerNameToIdMap.TryGetValue(markerName, out markerId))
                    throw new InvalidOperationException(String.Format("Maker '{0}' is not registered." + "Make sure you specifed same name as you used for BeginMark" + " method.", markerName));

                var markerIdx = level.MarkerNests[--level.NestCount];
                if (level.Markers[markerIdx].MarkerId != markerId)
                    throw new InvalidOperationException("Incorrect call order of BeginMark/EndMark method." + "You call it like BeginMark(A), BeginMark(B), EndMark(B), EndMark(A)" + " But you can't call it like " + "BeginMark(A), BeginMark(B), EndMark(A), EndMark(B).");
                
                level.Markers[markerIdx].EndTime = (float)_stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Get average time of given level index and marker name.
        /// </summary>
        /// <param name="levelIndex">Index of level</param>
        /// <param name="markerName">name of marker</param>
        /// <returns>average spending time in ms.</returns>
        public float GetAverageTimeInMilliseconds(int levelIndex, string markerName)
        {
            if (levelIndex < 0 || levelIndex >= MaxLevels)
                throw new ArgumentOutOfRangeException("levelIndex");

            float result = 0;
            int markerId;
            if (_markerNameToIdMap.TryGetValue(markerName, out markerId))
                result = _markers[markerId].Logs[levelIndex].Avg;

            return result;
        }

        public void ResetMarkerLog()
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

        public bool DoUpdate()
        {
            if (Sleep)
                Thread.Sleep(1);

            return !SkipUpdate;
        }

        public override void Update(GameTime gameTime)
        {
            if (_locationJustChanged)
            {
                _locationJustChanged = false;
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

            TimeRuler.Draw(_prevLog);
            PerformanceGraph.Draw(_internalLabeler, _prevLog);
        }
    }
}
