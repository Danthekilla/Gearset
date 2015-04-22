using System;
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace Gearset.Components.Profiler
{
    public abstract class UIView : UI.Window
#if WINDOWS
        , INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(String name)
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

        internal event EventHandler VisibleChanged;

        bool _visible;

        int _startLevelIndex;
        int _endLevelIndex;

        protected UIView(Vector2 position, Vector2 size) : base(position, size) { }

        public int StartLevelIndex
        {
            get { return _startLevelIndex; }
            set { _startLevelIndex = (int)MathHelper.Clamp(value, 0, Math.Min(_endLevelIndex, Profiler.MaxLevels - 1)); }
        }

        public int EndLevelIndex
        {
            get { return _endLevelIndex; }
            set { _endLevelIndex = (int)MathHelper.Clamp(value, Math.Max(0, _startLevelIndex), Profiler.MaxLevels - 1); }
        }

        protected bool DrawLevel(int level)
        {
            return level >= _startLevelIndex && level <= _endLevelIndex;
        }
    }
}
