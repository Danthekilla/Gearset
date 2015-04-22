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

        protected Profiler Profiler;

        protected UIView(Profiler profiler, Vector2 position, Vector2 size) : base(position, size) 
        {
            Profiler = profiler;
        }
    }
}
