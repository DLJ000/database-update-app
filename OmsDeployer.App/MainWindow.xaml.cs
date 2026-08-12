using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using OmsDeployer.Core;
using OmsDeployer.Core.Models;
using OmsDeployer.Core.Services;
using System.Windows.Forms;

namespace OmsDeployer.App
{
    public partial class MainWindow : Window
    {
        private DeploymentConfig _config;
        private CredentialService _credentialService;
        private Logger _logger;
        private BuildService _buildService;
        private ScpService _scpService;
        private SshService _sshService;
        private UiBuildService _uiBuildService;

        private const string PasswordPlaceholder = "••••••••";
        private bool _tomcatPasswordIsPlaceholder = false;

        public MainWindow()
        {
            InitializeComponent();
            _config = new DeploymentConfig();
            _credentialService = new CredentialService();
            _logger = new Logger();
            _buildService = new BuildService(_logger);
            _scpService = new ScpService(_logger);
            _sshService = new SshService(_logger);
            _uiBuildService = new UiBuildService(_logger);

            LoadConfig();
            ProfileComboBox.SelectionChanged += ProfileComboBox_SelectionChanged;
            UiProfileComboBox.SelectionChanged += UiProfileComboBox_SelectionChanged;
            UiPlatformComboBox.SelectionChanged += (s, e) => UpdateUiTab();
            UpdateUI();
            UpdateUiTab();
        }

        private void LoadConfig()
        {
            // Load saved config from settings
            _config.RepoPath = Properties.Settings.Default.RepoPath ?? "C:\\Users\\darle\\Documents\\repo";
            _config.UiRepoPath = Properties.Settings.Default.UiRepoPath ?? "C:\\Users\\darle\\Documents\\uirepo";

            _config.TomcatPassword = _credentialService.LoadCredentials();

            RepoPathTextBox.Text = _config.RepoPath;
            UiRepoPathTextBox.Text = _config.UiRepoPath;

            if (!string.IsNullOrEmpty(_config.TomcatPassword))
            {
                TomcatPasswordBox.Password = PasswordPlaceholder;
                _tomcatPasswordIsPlaceholder = true;
            }

            TomcatPasswordBox.PasswordChanged += (s, e) => { if (_tomcatPasswordIsPlaceholder && TomcatPasswordBox.Password != PasswordPlaceholder) _tomcatPasswordIsPlaceholder = false; };

            ScanProfiles();
            ScanUiProfiles();
        }

        private void ScanProfiles()
        {
            if (string.IsNullOrEmpty(_config.RepoPath) || !Directory.Exists(_config.RepoPath))
                return;

            var profiles = ProfileScanner.ScanProfiles(_config.RepoPath);
            ProfileComboBox.Items.Clear();
            foreach (var profile in profiles)
                ProfileComboBox.Items.Add(profile);

            if (ProfileComboBox.Items.Count > 0)
                ProfileComboBox.SelectedIndex = 0;
        }

        private void ScanUiProfiles()
        {
            if (string.IsNullOrEmpty(_config.UiRepoPath) || !Directory.Exists(_config.UiRepoPath))
                return;

            var profiles = UiProfileScanner.ScanProfiles(_config.UiRepoPath);
            UiProfileComboBox.Items.Clear();
            foreach (var profile in profiles)
                UiProfileComboBox.Items.Add(profile);

            if (UiProfileComboBox.Items.Count > 0)
                UiProfileComboBox.SelectedIndex = 0;
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UiProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUiTab();
        }

        private void UpdateUI()
        {
            var hasProfile = ProfileComboBox.SelectedItem != null;
            var hasRepoPath = !string.IsNullOrEmpty(_config.RepoPath) && Directory.Exists(_config.RepoPath);
            var hasTomcatCreds = !string.IsNullOrEmpty(_config.TomcatPassword);

            var canBuild = hasRepoPath && hasProfile;
            var canUpload = canBuild && hasTomcatCreds;

            BuildButton.IsEnabled = canBuild;
            UploadButton.IsEnabled = canUpload;
            DeployButton.IsEnabled = canBuild;
        }

        private void UpdateUiTab()
        {
            var hasProfile = UiProfileComboBox.SelectedItem != null;
            var hasRepoPath = !string.IsNullOrEmpty(_config.UiRepoPath) && Directory.Exists(_config.UiRepoPath);
            var hasTomcatCreds = !string.IsNullOrEmpty(_config.TomcatPassword);

            var isRfLambda = UiPlatformComboBox.SelectedIndex == 0;

            UiBuildButton.IsEnabled = hasRepoPath && hasProfile;
            UiUploadButton.IsEnabled = hasRepoPath && hasProfile && hasTomcatCreds;
            UiDeployButton.IsEnabled = hasRepoPath && hasProfile && isRfLambda;
        }

        private void BrowseRepoPath(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _config.RepoPath = dialog.SelectedPath;
                RepoPathTextBox.Text = _config.RepoPath;
                Properties.Settings.Default.RepoPath = _config.RepoPath;
                Properties.Settings.Default.Save();
                ScanProfiles();
                UpdateUI();
            }
        }

        private void BrowseUiRepoPath(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _config.UiRepoPath = dialog.SelectedPath;
                UiRepoPathTextBox.Text = _config.UiRepoPath;
                Properties.Settings.Default.UiRepoPath = _config.UiRepoPath;
                Properties.Settings.Default.Save();
                ScanUiProfiles();
                UpdateUiTab();
            }
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            _config.RepoPath = RepoPathTextBox.Text;
            _config.UiRepoPath = UiRepoPathTextBox.Text;

            var savedTomcatPwd = _credentialService.LoadCredentials();
            string tomcatPwd = _tomcatPasswordIsPlaceholder ? savedTomcatPwd : TomcatPasswordBox.Password;

            _credentialService.SaveCredentials(tomcatPwd);

            _config.TomcatPassword = tomcatPwd;

            Properties.Settings.Default.RepoPath = _config.RepoPath;
            Properties.Settings.Default.UiRepoPath = _config.UiRepoPath;
            Properties.Settings.Default.Save();

            ScanProfiles();
            ScanUiProfiles();
            UpdateUI();
            UpdateUiTab();

            StatusTextBlock.Text = "Settings saved.";
        }

        private static Platform ParsePlatform(int selectedIndex) => selectedIndex switch
        {
            0 => Platform.RfLambda,
            1 => Platform.RapidRf,
            2 => Platform.MillerMmic,
            3 => Platform.DBWave_Tomcat9,
            _ => Platform.RfLambda
        };

        private async void BuildWar(object sender, RoutedEventArgs e)
        {
            _config.ProfileName = ProfileComboBox.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(_config.ProfileName))
            {
                System.Windows.MessageBox.Show("Please select a profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BuildButton.IsEnabled = false;
            LogTextBox.Clear();
            StatusTextBlock.Text = "Building...";

            var progress = new Progress<string>(msg =>
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });

            var success = await _buildService.BuildWar(_config, progress);
            StatusTextBlock.Text = success ? "Build Complete" : "Build Failed";
            BuildButton.IsEnabled = true;
        }

        private async void UploadWar(object sender, RoutedEventArgs e)
        {
            _config.ProfileName = ProfileComboBox.SelectedItem?.ToString() ?? "";
            _config.Platform = ParsePlatform(PlatformComboBox.SelectedIndex);
            UploadButton.IsEnabled = false;
            StatusTextBlock.Text = "Uploading...";

            var progress = new Progress<string>(msg =>
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });

            var success = await _scpService.UploadWar(_config, _config.ProfileName, progress);
            StatusTextBlock.Text = success ? "Upload Complete" : "Upload Failed";
            UploadButton.IsEnabled = true;
        }

        private async void DeployWar(object sender, RoutedEventArgs e)
        {
            _config.ProfileName = ProfileComboBox.SelectedItem?.ToString() ?? "";
            _config.Platform = ParsePlatform(PlatformComboBox.SelectedIndex);

            var result = System.Windows.MessageBox.Show(
                $"Deploy {_config.ProfileName} to {_config.Platform}?\n\nThis will backup the current WAR and deploy the new one.",
                "Confirm Deployment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes)
                return;

            DeployButton.IsEnabled = false;
            StatusTextBlock.Text = "Deploying...";

            var progress = new Progress<string>(msg =>
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });

            var success = await _sshService.Deploy(_config, _config.ProfileName, progress);
            StatusTextBlock.Text = success ? "Deployment Complete" : "Deployment Failed";
            DeployButton.IsEnabled = true;
        }

        // --- Frontend UI Update handlers ---

        private async void UiBuildWar(object sender, RoutedEventArgs e)
        {
            var profileName = UiProfileComboBox.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(profileName))
            {
                System.Windows.MessageBox.Show("Please select a profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UiBuildButton.IsEnabled = false;
            UiLogTextBox.Clear();
            StatusTextBlock.Text = "Building UI...";

            var progress = new Progress<string>(msg =>
            {
                UiLogTextBox.AppendText(msg + Environment.NewLine);
                UiLogTextBox.ScrollToEnd();
            });

            var success = await _uiBuildService.BuildWar(_config.UiRepoPath, profileName, progress);
            StatusTextBlock.Text = success ? "UI Build Complete" : "UI Build Failed";
            UiBuildButton.IsEnabled = true;
        }

        private async void UiDeployWar(object sender, RoutedEventArgs e)
        {
            var profileName = UiProfileComboBox.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(profileName))
            {
                System.Windows.MessageBox.Show("Please select a profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var localWarPath = _uiBuildService.FindWarFile(_config.UiRepoPath, profileName);
            if (string.IsNullOrEmpty(localWarPath))
            {
                System.Windows.MessageBox.Show(
                    $"No WAR file found in {Path.Combine(_config.UiRepoPath, profileName, "target")}.\nPlease build first.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var warFileName = Path.GetFileName(localWarPath);
            _config.Platform = ParsePlatform(UiPlatformComboBox.SelectedIndex);

            var result = System.Windows.MessageBox.Show(
                $"Deploy {profileName} UI to RfLambda?\n\nThis will backup ROOT.war, shutdown Tomcat, deploy, and restart.",
                "Confirm UI Deployment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            UiDeployButton.IsEnabled = false;
            UiLogTextBox.Clear();
            StatusTextBlock.Text = "Deploying UI...";

            var progress = new Progress<string>(msg =>
            {
                UiLogTextBox.AppendText(msg + Environment.NewLine);
                UiLogTextBox.ScrollToEnd();
            });

            var success = await _sshService.DeployUi(_config, warFileName, progress);
            StatusTextBlock.Text = success ? "UI Deploy Complete" : "UI Deploy Failed";
            UiDeployButton.IsEnabled = true;
        }

        private async void UiUploadWar(object sender, RoutedEventArgs e)
        {
            var profileName = UiProfileComboBox.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrEmpty(profileName))
            {
                System.Windows.MessageBox.Show("Please select a profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var localWarPath = _uiBuildService.FindWarFile(_config.UiRepoPath, profileName);
            if (string.IsNullOrEmpty(localWarPath))
            {
                System.Windows.MessageBox.Show(
                    $"No WAR file found in {Path.Combine(_config.UiRepoPath, profileName, "target")}.\nPlease build first.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _config.Platform = ParsePlatform(UiPlatformComboBox.SelectedIndex);

            UiUploadButton.IsEnabled = false;
            StatusTextBlock.Text = "Uploading UI WAR...";

            var progress = new Progress<string>(msg =>
            {
                UiLogTextBox.AppendText(msg + Environment.NewLine);
                UiLogTextBox.ScrollToEnd();
            });

            var success = await _scpService.UploadWarFromPath(_config, localWarPath, progress);
            StatusTextBlock.Text = success ? "UI Upload Complete" : "UI Upload Failed";
            UiUploadButton.IsEnabled = true;
        }
    }
}
