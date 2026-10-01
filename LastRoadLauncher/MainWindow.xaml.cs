using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace LastRoadLauncher
{
    public partial class MainWindow : Window
    {
        private const string UpdateBaseUrl =
            "https://last-road.com/updates/";

        private const string ManifestUrl =
            UpdateBaseUrl + "manifest.json";

        private static readonly HttpClient Http = new HttpClient();

        private Manifest? _manifest;

        private bool _languageSettingsLoaded;

        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "LastRoadLauncher");

        private static readonly string SettingsPath =
            Path.Combine(
                SettingsDirectory,
                "settings.json");

        public MainWindow()
        {
            InitializeComponent();

            EnsureUserDirectories();

            LoadLanguageSettings();

            Loaded += MainWindow_Loaded;
        }

        private static void EnsureUserDirectories()
        {
            string screenshotsDirectory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Screenshots");

            Directory.CreateDirectory(
                screenshotsDirectory);
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await LoadManifestAsync();
        }

        private void LoadLanguageSettings()
        {
            string language = "ua";

            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json =
                        File.ReadAllText(SettingsPath);

                    LauncherSettings? settings =
                        JsonSerializer.Deserialize<LauncherSettings>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (settings != null &&
                        string.Equals(
                            settings.Language,
                            "en",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        language = "en";
                    }
                }
            }
            catch
            {
                // If the settings file is missing, damaged,
                // or unreadable, UA is used by default.
                language = "ua";
            }

            _languageSettingsLoaded = false;

            if (language == "en")
            {
                EnLanguage.IsChecked = true;
            }
            else
            {
                UaLanguage.IsChecked = true;
            }

            _languageSettingsLoaded = true;
        }

        private void Language_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (!_languageSettingsLoaded)
                return;

            SaveLanguageSettings();
        }

        private void SaveLanguageSettings()
        {
            try
            {
                Directory.CreateDirectory(
                    SettingsDirectory);

                LauncherSettings settings =
                    new LauncherSettings
                    {
                        Language =
                            UaLanguage.IsChecked == true
                                ? "ua"
                                : "en"
                    };

                string json =
                    JsonSerializer.Serialize(
                        settings,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    SettingsPath,
                    json);
            }
            catch
            {
                // Saving the language preference must never
                // prevent the launcher from working.
            }
        }

        private async Task LoadManifestAsync()
        {
            try
            {
                StatusText.Text = "Client status: Connecting...";
                UpdateProgress.Value = 0;

                string json =
                    await Http.GetStringAsync(ManifestUrl);

                _manifest =
                    JsonSerializer.Deserialize<Manifest>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (_manifest == null)
                    throw new Exception("Invalid manifest.");

                StatusText.Text =
                    $"Connected · Version {_manifest.ClientVersion}";

                UpdateProgress.Value = 100;
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Update server unavailable";

                UpdateProgress.Value = 0;

                MessageBox.Show(
                    ex.Message,
                    "LAST ROAD Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private List<ManifestFile> GetSelectedFiles()
        {
            if (_manifest == null)
                return new List<ManifestFile>();

            List<ManifestFile> files =
                new List<ManifestFile>();

            // Common client files
            files.AddRange(_manifest.Files);

            // Selected language
            string language =
                UaLanguage.IsChecked == true
                    ? "ua"
                    : "en";

            if (_manifest.Languages.TryGetValue(
                language,
                out List<ManifestFile>? languageFiles))
            {
                files.AddRange(languageFiles);
            }

            return files;
        }

        private async void UpdateButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_manifest == null)
            {
                await LoadManifestAsync();

                if (_manifest == null)
                    return;
            }

            SetButtonsEnabled(false);

            try
            {
                List<ManifestFile> files =
                    GetSelectedFiles();

                int total = files.Count;

                if (total == 0)
                {
                    StatusText.Text =
                        "Client is up to date";

                    UpdateProgress.Value = 100;
                    return;
                }

                List<(ManifestFile File, string LocalPath)> filesToDownload =
                    new List<(ManifestFile File, string LocalPath)>();

                long totalDownloadBytes = 0;

                UpdateProgress.Value = 0;

                // Fast pre-scan: only existence and size are checked.
                // This lets the launcher know the real amount of data
                // that must be downloaded before downloading starts.
                for (int i = 0; i < total; i++)
                {
                    ManifestFile file = files[i];

                    StatusText.Text =
                        $"Quick check {i + 1}/{total}: {file.Path}";

                    string localPath =
                        GetSafeLocalPath(file.Path);

                    // Option.ini contains the player's personal
                    // graphics and display settings.
                    //
                    // If it already exists, UPDATE must never
                    // overwrite or modify it.
                    //
                    // If it does not exist, the default Option.ini
                    // from the update server will be downloaded.
                    if (IsUserOptionFile(file.Path) &&
                        File.Exists(localPath))
                    {
                        continue;
                    }

                    bool needsUpdate = true;

                    if (File.Exists(localPath))
                    {
                        FileInfo fileInfo =
                            new FileInfo(localPath);

                        needsUpdate =
                            fileInfo.Length != file.Size;
                    }

                    if (needsUpdate)
                    {
                        filesToDownload.Add(
                            (file, localPath));

                        totalDownloadBytes += file.Size;
                    }
                }

                string languageName =
                    GetSelectedLanguageName();

                if (filesToDownload.Count == 0)
                {
                    	MarkSelectedClientInstalled();
   			RefreshClientInstallationState();
			StatusText.Text =
                        $"Client is up to date · {languageName}";

                    UpdateProgress.Value = 100;
                    return;
                }
		
		if (!HasEnoughDiskSpace(
    totalDownloadBytes,
    out long freeBytes,
    out long requiredBytes))
{
    StatusText.Text =
        "Not enough disk space";

    MessageBox.Show(
        $"Not enough free disk space.\n\n" +
        $"Required: {FormatBytes(requiredBytes)}\n" +
        $"Available: {FormatBytes(freeBytes)}\n\n" +
        $"Please free some disk space and try again.",
        "LAST ROAD Launcher",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);

    UpdateProgress.Value = 0;
    return;
}

                long downloadedBytes = 0;
                int downloadedFiles = 0;

                Stopwatch downloadTimer =
                    Stopwatch.StartNew();

                UpdateProgress.Value = 0;

                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    ManifestFile file =
                        filesToDownload[i].File;

                    string localPath =
                        filesToDownload[i].LocalPath;

                    await DownloadAndVerifyAsync(
                        file,
                        localPath,
                        bytesRead =>
                        {
                            downloadedBytes += bytesRead;

                            double progress =
                                totalDownloadBytes > 0
                                    ? (double)downloadedBytes /
                                      totalDownloadBytes * 100
                                    : 100;

                            UpdateProgress.Value =
                                Math.Min(progress, 100);

                            double elapsedSeconds =
                                Math.Max(
                                    downloadTimer.Elapsed.TotalSeconds,
                                    0.001);

                            double bytesPerSecond =
                                downloadedBytes /
                                elapsedSeconds;

                            StatusText.Text =
                                $"Downloading {FormatBytes(downloadedBytes)} / " +
                                $"{FormatBytes(totalDownloadBytes)} · " +
                                $"{progress:0.0}% · " +
                                $"{FormatSpeed(bytesPerSecond)}";
                        });

                    downloadedFiles++;
                }

                downloadTimer.Stop();

		MarkSelectedClientInstalled();

                StatusText.Text =
                    $"Update complete · {languageName} · Downloaded: {downloadedFiles}";

                UpdateProgress.Value = 100;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Update failed";

                MessageBox.Show(
                    ex.Message,
                    "LAST ROAD Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
                RefreshClientInstallationState();
            }
        }

        private async void RepairButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_manifest == null)
            {
                await LoadManifestAsync();

                if (_manifest == null)
                    return;
            }

            SetButtonsEnabled(false);

            try
            {
                List<ManifestFile> files =
                    GetSelectedFiles();

                int total = files.Count;

                if (total == 0)
                {
                    StatusText.Text =
                        "Nothing to repair";

                    UpdateProgress.Value = 100;
                    return;
                }

                int repairedFiles = 0;

                UpdateProgress.Value = 0;

                for (int i = 0; i < total; i++)
                {
                    ManifestFile file = files[i];

                    StatusText.Text =
                        $"Repair checking {i + 1}/{total}: {file.Path}";

                    UpdateProgress.Value =
                        (double)i / total * 100;

                    string localPath =
                        GetSafeLocalPath(file.Path);

                    // Option.ini is user data.
                    //
                    // If it already exists, REPAIR must not hash,
                    // replace or restore it because doing so would
                    // reset the player's personal settings.
                    //
                    // If it is missing, REPAIR restores our
                    // default Option.ini.
                    if (IsUserOptionFile(file.Path) &&
                        File.Exists(localPath))
                    {
                        UpdateProgress.Value =
                            (double)(i + 1) / total * 100;

                        continue;
                    }

                    bool valid =
                        File.Exists(localPath) &&
                        await VerifySha256Async(
                            localPath,
                            file.Sha256);

                    if (!valid)
                    {
                        StatusText.Text =
                            $"Repairing {i + 1}/{total}: {file.Path}";

                        await DownloadAndVerifyAsync(
                            file,
                            localPath);

                        repairedFiles++;
                    }

                    UpdateProgress.Value =
                        (double)(i + 1) / total * 100;
                }

                string languageName =
                    GetSelectedLanguageName();

                if (repairedFiles == 0)
                {
                    StatusText.Text =
                        $"Repair complete · {languageName} · No problems found";
                }
                else
                {
                    StatusText.Text =
                        $"Repair complete · {languageName} · Restored: {repairedFiles}";
                }

                UpdateProgress.Value = 100;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Repair failed";

                MessageBox.Show(
                    ex.Message,
                    "LAST ROAD Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private static bool IsUserOptionFile(
            string relativePath)
        {
            string normalizedPath =
                relativePath.Replace('\\', '/');

            string fileName =
                Path.GetFileName(normalizedPath);

            return string.Equals(
                fileName,
                "Option.ini",
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task DownloadAndVerifyAsync(
            ManifestFile file,
            string localPath,
            Action<long>? progressCallback = null)
        {
            string? directory =
                Path.GetDirectoryName(localPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string downloadUrl =
                new Uri(
                    new Uri(UpdateBaseUrl),
                    file.Url)
                .ToString();

            string temporaryPath =
                localPath + ".download";

            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                using HttpResponseMessage response =
                    await Http.GetAsync(
                        downloadUrl,
                        HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                await using (Stream downloadStream =
                    await response.Content.ReadAsStreamAsync())
                {
                    await using FileStream fileStream =
                        new FileStream(
                            temporaryPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            1024 * 1024,
                            useAsync: true);

                    byte[] buffer =
                        new byte[1024 * 1024];

                    while (true)
                    {
                        int bytesRead =
                            await downloadStream.ReadAsync(
                                buffer.AsMemory(
                                    0,
                                    buffer.Length));

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        await fileStream.WriteAsync(
                            buffer.AsMemory(
                                0,
                                bytesRead));

                        progressCallback?.Invoke(
                            bytesRead);
                    }

                    await fileStream.FlushAsync();
                }

                bool downloadedFileValid =
                    await VerifySha256Async(
                        temporaryPath,
                        file.Sha256);

                if (!downloadedFileValid)
                {
                    throw new Exception(
                        $"SHA-256 verification failed: {file.Path}");
                }

                File.Move(
                    temporaryPath,
                    localPath,
                    overwrite: true);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }

                throw;
            }
        }
	
	private static bool HasEnoughDiskSpace(
    long downloadBytes,
    out long freeBytes,
    out long requiredBytes)
{
    const long safetyReserve =
        1L * 1024 * 1024 * 1024;

    requiredBytes =
        downloadBytes + safetyReserve;

    string clientRoot =
        Path.GetFullPath(
            AppContext.BaseDirectory);

    string? rootPath =
        Path.GetPathRoot(clientRoot);

    if (string.IsNullOrEmpty(rootPath))
    {
        freeBytes = 0;
        return false;
    }

    DriveInfo drive =
        new DriveInfo(rootPath);

    freeBytes =
        drive.AvailableFreeSpace;

    return freeBytes >= requiredBytes;
}

        private static string FormatBytes(long bytes)
        {
            const double oneGb =
                1024.0 * 1024.0 * 1024.0;

            const double oneMb =
                1024.0 * 1024.0;

            if (bytes >= oneGb)
            {
                return $"{bytes / oneGb:0.00} GB";
            }

            return $"{bytes / oneMb:0.00} MB";
        }

        private static string FormatSpeed(
            double bytesPerSecond)
        {
            const double oneMb =
                1024.0 * 1024.0;

            return
                $"{bytesPerSecond / oneMb:0.0} MB/s";
        }

        private static string GetSafeLocalPath(
            string relativePath)
        {
            string clientRoot =
                Path.GetFullPath(
                    AppContext.BaseDirectory);

            string localPath =
                Path.GetFullPath(
                    Path.Combine(
                        clientRoot,
                        relativePath));

            if (!localPath.StartsWith(
                clientRoot,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    $"Invalid manifest path: {relativePath}");
            }

            return localPath;
        }

        private string GetSelectedLanguageName()
        {
            return UaLanguage.IsChecked == true
                ? "UA"
                : "EN";
        }

        private void SetButtonsEnabled(bool enabled)
        {
            UpdateButton.IsEnabled = enabled;
            RepairButton.IsEnabled = enabled;
            PlayButton.IsEnabled = enabled;

            UaLanguage.IsEnabled = enabled;
            EnLanguage.IsEnabled = enabled;
        }

        private void PlayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                SaveLanguageSettings();

                string systemFolder =
                    UaLanguage.IsChecked == true
                        ? "system - UA"
                        : "system - EN";

                string clientRoot =
                    AppContext.BaseDirectory;

                string systemPath =
                    Path.Combine(
                        clientRoot,
                        systemFolder);

                string l2Path =
                    Path.Combine(
                        systemPath,
                        "L2.exe");

                if (!File.Exists(l2Path))
                {
                    MessageBox.Show(
                        $"L2.exe not found.\n\nExpected location:\n{l2Path}",
                        "LAST ROAD Launcher",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    StatusText.Text =
                        $"{systemFolder}: L2.exe not found";

                    return;
                }

                ProcessStartInfo startInfo =
                    new ProcessStartInfo
                    {
                        FileName = l2Path,
                        WorkingDirectory = systemPath,
                        UseShellExecute = true
                    };

                Process.Start(startInfo);

                StatusText.Text =
                    $"Starting {systemFolder}...";

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Game launch failed";

                MessageBox.Show(
                    ex.Message,
                    "LAST ROAD Launcher",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static async Task<bool> VerifySha256Async(
            string path,
            string expectedHash)
        {
            await using FileStream stream =
                File.OpenRead(path);

            using SHA256 sha = SHA256.Create();

            byte[] hash =
                await sha.ComputeHashAsync(stream);

            string actualHash =
                Convert.ToHexString(hash)
                    .ToLowerInvariant();

            return string.Equals(
                actualHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public class LauncherSettings
    {
        public string Language { get; set; } = "ua";
    }

    public class Manifest
    {
        public string LauncherVersion { get; set; } = "";
        public string ClientVersion { get; set; } = "";

        public List<ManifestFile> Files { get; set; } =
            new();

        public Dictionary<string, List<ManifestFile>>
            Languages { get; set; } =
                new();
    }

    public class ManifestFile
    {
        public string Path { get; set; } = "";
        public string Url { get; set; } = "";
        public long Size { get; set; }
        public string Sha256 { get; set; } = "";
    }
}
