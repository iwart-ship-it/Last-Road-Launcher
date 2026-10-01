using System;
using System.IO;
using System.Windows;

namespace LastRoadLauncher
{
    public partial class MainWindow
    {
        private bool _localizationInitialized;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_localizationInitialized)
            {
                return;
            }

            _localizationInitialized = true;

            UaLanguage.Checked += InterfaceLanguage_Checked;
            EnLanguage.Checked += InterfaceLanguage_Checked;

            ApplyInterfaceLanguage();
        }

        private void InterfaceLanguage_Checked(
            object sender,
            RoutedEventArgs e)
        {
            ApplyInterfaceLanguage();
        }

        private void ApplyInterfaceLanguage()
        {
            bool isEnglish =
                EnLanguage.IsChecked == true;

            bool clientInstalled =
                IsSelectedClientInstalled();

            if (isEnglish)
            {
                UpdateButton.Content =
                    clientInstalled
                        ? "UPDATE"
                        : "INSTALL";

                RepairButton.Content = "REPAIR";
                PlayButton.Content = "PLAY";
            }
            else
            {
                UpdateButton.Content =
                    clientInstalled
                        ? "ОНОВИТИ"
                        : "ВСТАНОВИТИ";

                RepairButton.Content = "ВІДНОВИТИ";
                PlayButton.Content = "ГРАТИ";
            }

            PlayButton.IsEnabled = clientInstalled;
            RepairButton.IsEnabled = clientInstalled;
        }

        private string GetSelectedInstallMarkerPath()
        {
            string markerName =
                UaLanguage.IsChecked == true
                    ? ".lastroad-installed-ua"
                    : ".lastroad-installed-en";

            return Path.Combine(
                AppContext.BaseDirectory,
                markerName);
        }

        private bool IsSelectedClientInstalled()
        {
            string systemFolder =
                UaLanguage.IsChecked == true
                    ? "system - UA"
                    : "system - EN";

            string l2Path =
                Path.Combine(
                    AppContext.BaseDirectory,
                    systemFolder,
                    "L2.exe");

            string markerPath =
                GetSelectedInstallMarkerPath();

            return
                File.Exists(l2Path) &&
                File.Exists(markerPath);
        }

        private void MarkSelectedClientInstalled()
        {
            string markerPath =
                GetSelectedInstallMarkerPath();

            File.WriteAllText(
                markerPath,
                "Last Road client installation completed.");
        }

        private void RefreshClientInstallationState()
        {
            ApplyInterfaceLanguage();
        }
    }
}