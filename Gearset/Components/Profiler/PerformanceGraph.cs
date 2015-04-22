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
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TimeRuler"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                if (VisibleChanged != null)
                    VisibleChanged(this, EventArgs.Empty);
#if WINDOWS
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
#endif
            }
        }
        private bool _visible;

        internal event EventHandler VisibleChanged;

        const int MaxFrames = 40;
        private readonly Queue<float> _timings = new Queue<float>(MaxFrames);

        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        internal PerformanceGraph(int sampleFrames, Vector2 position, Vector2 size) : base(position, size)
        {
            for (var i = 0; i < MaxFrames; i++)
                _timings.Enqueue(0);
        }

        int _c;
        internal void Draw(InternalLabeler labeler, Profiler.FrameLog frameLog)
        {
            if (Visible == false)
            {
                labeler.HideLabel("__performanceGraph");
                return;
            }

            DrawBorderLines(Color.Gray);//, _lineDrawer);
            //if (TitleBar.IsMouseOver)
            //    TitleBar.DrawBorderLines(Color.White);//, _lineDrawer);
            if (ScaleNob.IsMouseOver)
                ScaleNob.DrawBorderLines(Color.Gray);//, _lineDrawer);

            labeler.ShowLabel("__performanceGraph", Position + new Vector2(0, -12), "Performance Graph");
            
            _c++;
            if (_c >= 10)
            {
                _c = 0;

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


            var barWidth = 2 * Width / MaxFrames;
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
