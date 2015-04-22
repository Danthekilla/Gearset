using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    [Serializable]
    public class ProfilerConfig : GearConfig
    {
        [InspectorIgnore]
        public double Top { get; internal set; }
        [InspectorIgnore]
        public double Left { get; internal set; }
        [InspectorIgnore]
        public double Width { get; internal set; }
        [InspectorIgnore]
        public double Height { get; internal set; }

        [InspectorIgnore]
        public List<String> HiddenStreams { get; internal set; }

        [InspectorIgnore]
        public Vector2 TimeRulerPosition { get; internal set; }
        [InspectorIgnore]
        public Vector2 TimeRulerSize { get; internal set; }
        [InspectorIgnore]
        public bool TimeRulerVisible { get; internal set; }

        [InspectorIgnore]
        public Vector2 PerformanceGraphPosition { get; internal set; }
        [InspectorIgnore]
        public Vector2 PerformanceGraphSize { get; internal set; }
        [InspectorIgnore]
        public bool PerformanceGraphVisible { get; internal set; }

        public ProfilerConfig()
        {
            // Defaults
            Top = 300;
            Left = 40;
            Width = 700;
            Height = 340;

            HiddenStreams = new List<string>();

            TimeRulerPosition = new Vector2(3, 3);
            TimeRulerSize = new Vector2(400, 16);
            TimeRulerVisible = true;

            PerformanceGraphPosition = new Vector2(3, 20);
            PerformanceGraphSize = new Vector2(100, 60);
            PerformanceGraphVisible = true;
        }
    }
}
