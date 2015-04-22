using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    public class PerformanceGraph : UIView
    {
        class Frame
        {
            public readonly List<TimingInfo> TimingInfos = new List<TimingInfo>(16);
        }

        struct TimingInfo
        {
            public int Level;
            public readonly float StartMilliseconds;
            public readonly float EndMilliseconds;
            public readonly Color Color;

            internal TimingInfo(int level, float startMilliseconds, float endMilliseconds, Color color)
            {
                Level = level;
                StartMilliseconds = startMilliseconds;
                EndMilliseconds = endMilliseconds;
                Color = color;
            }
        }

        const int MaxFrames = 60;

        private readonly Queue<Frame> _frames = new Queue<Frame>(MaxFrames);

        public ProfilerConfig Config { get { return GearsetSettings.Instance.ProfilerConfig; } }

        internal PerformanceGraph(Profiler profiler, Vector2 position, Vector2 size) : base(profiler, position, size)
        {
            for (var i = 0; i < MaxFrames; i++)
                _frames.Enqueue(new Frame());
        }

        int _c;
        internal void Draw(InternalLabeler labeler, Profiler.FrameLog frameLog)
        {
            if (Visible == false)
            {
                labeler.HideLabel("__performanceGraph");
                return;
            }

            DrawBorderLines(Color.Gray);

            if (ScaleNob.IsMouseOver)
                ScaleNob.DrawBorderLines(Color.Gray);

            labeler.ShowLabel("__performanceGraph", Position + new Vector2(0, -12), "Performance Graph");
            
            _c++;
            if (_c >= 0)
            {
                _c = 0;

                var bars = frameLog.Levels.Length;

                if (_frames.Count == MaxFrames)
                    _frames.Dequeue();

                var frame = new Frame();
                _frames.Enqueue(frame);

                for (var barId = 0; barId < frameLog.Levels.Length; barId++)
                {
                    var bar = frameLog.Levels[barId];
                    for (var j = 0; j < bar.MarkCount; ++j)
                    {
                        frame.TimingInfos.Add(new TimingInfo(
                            barId,
                            bar.Markers[j].BeginTime,
                            bar.Markers[j].EndTime,
                            bar.Markers[j].Color));
                    }
                }
            }

            const float frameSpan = 1.0f / 60.0f * 1000f;

            GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(Position, Position + Size, new Color(56, 56, 56, 150), new Color(16, 16, 16, 127));

            var msToPs = Height / frameSpan;

            var barWidth = Width / MaxFrames;
            var graphFloor = Position.Y + Size.Y;
            var position = new Vector2(Position.X, graphFloor);

            var s = new Vector2(barWidth, msToPs);
            foreach (var frame in _frames)
            {
                foreach (var timeInfo in frame.TimingInfos)
                {
                    if (Levels[timeInfo.Level].Enabled == false)
                        continue;

                    var durationMilliseconds = timeInfo.EndMilliseconds - timeInfo.StartMilliseconds;
                    if (durationMilliseconds <= 0)
                        continue;

                    s.Y = -durationMilliseconds*msToPs;
                    position.Y = graphFloor - (timeInfo.StartMilliseconds * msToPs);

                    GearsetResources.Console.SolidBoxDrawer.ShowGradientBoxOnce(position, position + s,
                        timeInfo.Color, timeInfo.Color);
                }

                position.X += barWidth;
            }
        }
    }
}
