using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace LastRoadLauncher
{
    public partial class MainWindow
    {
        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void CooperationVideo_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenUrl("https://youtu.be/4c7IvlUNiFM");
        }

        private void InterludeVsClassicVideo_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenUrl("https://youtu.be/06Dg77flFCU");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Launcher should continue working
                // even if Windows cannot open the browser.
            }
        }
    }
}