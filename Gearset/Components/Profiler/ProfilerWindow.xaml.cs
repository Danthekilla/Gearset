using System;
using System.Windows;
using System.Windows.Input;

namespace Gearset.Components.Profiler
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ProfilerWindow : Window
    {
        internal bool WasHiddenByGameMinimize { get; set; }

        public ProfilerWindow()
        {
            InitializeComponent();

            Closing += ProfilerWindow_Closing;
        }

        internal event EventHandler<SoloRequestedEventArgs> SoloRequested;

        public void ProfilerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            GearsetResources.Console.SaveLogToFile();
        }

        public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public void Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Solo_Click(object sender, RoutedEventArgs e)
        {
            if (SoloRequested != null)
                SoloRequested(this, new SoloRequestedEventArgs((StreamItem)((FrameworkElement)e.OriginalSource).DataContext));
        }

        private void DisableAllButton_Click(object sender, RoutedEventArgs e)
        {
            GearsetResources.Console.Profiler.DisableAllStreams();
        }

        private void EnableAllButton_Click(object sender, RoutedEventArgs e)
        {
            GearsetResources.Console.Profiler.EnableAllStreams();
        }
    }

    internal class SoloRequestedEventArgs : EventArgs
    {
        internal StreamItem StreamItem { get; private set; }

        public SoloRequestedEventArgs(StreamItem item)
        {
            StreamItem = item;
        }
    }
}
