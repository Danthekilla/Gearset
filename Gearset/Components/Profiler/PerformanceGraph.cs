using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    public class PerformanceGraph : UI.Window
#if WINDOWS
        , INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
#else
    {
#endif
        const int MaxFrames = 40;
        private readonly Queue<float> _timings = new Queue<float>(MaxFrames);

        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        internal PerformanceGraph(int sampleFrames, Vector2 position, Vector2 size) : base(position, size)
        {
            for (var i = 0; i < MaxFrames; i++)
                _timings.Enqueue(0);
        }

        int c;
        internal void Draw(Profiler.FrameLog frameLog)
        {
            c++;
            if (c >= 3)
            {
                c = 0;

                if (_timings.Count == MaxFrames)
                {
                    _timings.Dequeue();
                    _timings.Dequeue();
                }
                _timings.Enqueue(frameLog.Bars[0].Markers[0].EndTime - frameLog.Bars[0].Markers[0].BeginTime);
                _timings.Enqueue(frameLog.Bars[0].Markers[1].EndTime - frameLog.Bars[0].Markers[1].BeginTime);
            }

            const float frameSpan = 1.0f / 60.0f * 1000f;

            GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(Position, Position + Size, new Color(56, 56, 56, 150), new Color(16, 16, 16, 127));

            // Compute factor that converts from ms to pixel.
            var msToPs = Height / frameSpan;


            const int barWidth = 30;
            var y = Position.Y + Size.Y;
            var position = new Vector2(Position.X, y);
            var s = new Vector2(barWidth, msToPs);
            var b = false;
            foreach (var value in _timings)
            {
                
                //if (value > 0)
                //{
                    s.Y = value * msToPs;
                    position.Y -= s.Y;
                    if (!b)
                    {
                        GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s, Color.Blue, Color.Blue);
                    }
                    else
                        GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s, Color.Red, Color.Red);
                //}    
                    b = !b;
                

                if (!b)
                {
                    position.X += barWidth;
                    position.Y = y;
                }
            }
        }
    }
}
