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
        public Vector2 PerformanceGraphPosition { get; internal set; }

        [InspectorIgnore]
        public Vector2 PerformanceGraphSize { get; internal set; }

        [InspectorIgnore]
        public List<String> HiddenStreams { get; internal set; }

        public ProfilerConfig()
        {
            // Defaults
            Top = 300;
            Left = 40;
            Width = 700;
            Height = 340;

            HiddenStreams = new List<string>();

            PerformanceGraphSize = new Vector2(100, 60);
        }
    }
}
