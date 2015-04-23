using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
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
        protected readonly Profiler Profiler;

        bool _visible;

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

        internal ObservableCollection<Profiler.LevelItem> Levels = new ObservableCollection<Profiler.LevelItem>();
        
        protected UIView(Profiler profiler, Vector2 position, Vector2 size) : base(position, size) 
        {
            Profiler = profiler;

            var textColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 221, 221, 221));

            for(var i = 0; i < Profiler.MaxLevels; i++)
            {
                var levelItem = new Profiler.LevelItem { Name = "Level " + (i + 1), Enabled = true, Color = textColor };
                Levels.Add(levelItem);

                levelItem.PropertyChanged += (sender, args) => { 
                    if (args.PropertyName == "Enabled")
                        LevelsChanged();
                };
            }
        }

        void LevelsChanged() 
        {
            var visibleLevelsMask = (byte)0;
            for (var levelId = 0; levelId < Levels.Count; levelId++)
            {
                var level = Levels[levelId];
                if (level.Enabled) 
                    visibleLevelsMask |= (byte)(1 << levelId);
            }

            _visibleLevelsMask = visibleLevelsMask;

            if (LevelsEnabledChanged != null)
                LevelsEnabledChanged(this, EventArgs.Empty);
        }

        internal event EventHandler LevelsEnabledChanged;

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

        byte _visibleLevelsMask;
        public byte VisibleLevelsMask
        {
            get 
            { 
                return _visibleLevelsMask; 
            }
            set 
            { 
                _visibleLevelsMask = value;
                for (var levelId = 0; levelId < Levels.Count; levelId++)
                {
                    var level = Levels[levelId];
                    level.Enabled = (_visibleLevelsMask & (byte)(1 << levelId)) != 0;
                }
            }
        }
    }
}
