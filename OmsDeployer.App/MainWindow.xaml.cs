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
        private FtpService _ftpService;
        private SshService _sshService;

        public MainWindow()
        {
            InitializeComponent();
            _config = new DeploymentConfig();
            _credentialService = new CredentialService();
            _logger = new Logger();
            _buildService = new BuildService(_logger);
            _ftpService = new FtpService(_logger);
            _sshService = new SshService(_logger);

            LoadConfig();
            ProfileComboBox.SelectionChanged += ProfileComboBox_SelectionChanged;
            UpdateUI();
        }

        private void LoadConfig()
        {
            // Load saved config from settings
            _config.RepoPath = Properties.Settings.Default.RepoPath ?? "C:\\Users\\darle\\Documents\\repo";
            _config.FtpHost = Properties.Settings.Default.FtpHost ?? "ftp.rflambda.com";
            _config.SshHost = Properties.Settings.Default.SshHost ?? "";
            
            var (ftpPwd, rootPwd, tomcatPwd) = _credentialService.LoadCredentials();
            _config.FtpPassword = ftpPwd;
            _config.RootPassword = rootPwd;
            _config.TomcatPassword = tomcatPwd;

            RepoPathTextBox.Text = _config.RepoPath;
            ScanProfiles();
        }

        private void ScanProfiles()
        {
            if (string.IsNullOrEmpty(_config.RepoPath) || !Directory.Exists(_config.RepoPath))
                return;

            var profiles = ProfileScanner.ScanProfiles(_config.RepoPath);
            ProfileComboBox.Items.Clear();
            foreach (var profile in profiles)
            {
                ProfileComboBox.Items.Add(profile);
            }

            if (ProfileComboBox.Items.Count > 0)
                ProfileComboBox.SelectedIndex = 0;
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var hasProfile = ProfileComboBox.SelectedItem != null;
            var hasRepoPath = !string.IsNullOrEmpty(_config.RepoPath) && Directory.Exists(_config.RepoPath);
            var hasFtpCreds = !string.IsNullOrEmpty(_config.FtpPassword);
            var hasSshCreds = !string.IsNullOrEmpty(_config.SshHost) && 
                             !string.IsNullOrEmpty(_config.RootPassword) && 
                             !string.IsNullOrEmpty(_config.TomcatPassword);

            var canBuild = hasRepoPath && hasProfile;
            var canUpload = canBuild && hasFtpCreds;
            var canStage = canUpload && hasSshCreds;
            var canDeploy = canStage;

            BuildButton.IsEnabled = canBuild;
            UploadButton.IsEnabled = canUpload;
            StageButton.IsEnabled = canStage;
            DeployButton.IsEnabled = canDeploy;
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

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new ConfigWindow(_config, _credentialService);
            if (settingsWindow.ShowDialog() == true)
            {
                // ConfigWindow already updated _config directly, just update UI
                UpdateUI();
            }
        }

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
            UploadButton.IsEnabled = false;
            StatusTextBlock.Text = "Uploading...";

            var progress = new Progress<string>(msg => 
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });

            var success = await _ftpService.UploadWar(_config, _config.ProfileName, progress);
            StatusTextBlock.Text = success ? "Upload Complete" : "Upload Failed";
            UploadButton.IsEnabled = true;
        }

        private async void StageWar(object sender, RoutedEventArgs e)
        {
            _config.ProfileName = ProfileComboBox.SelectedItem?.ToString() ?? "";
            StageButton.IsEnabled = false;
            StatusTextBlock.Text = "Staging...";

            var progress = new Progress<string>(msg => 
            {
                LogTextBox.AppendText(msg + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });

            var success = await _sshService.StageToTomcat(_config, _config.ProfileName, progress);
            StatusTextBlock.Text = success ? "Staging Complete" : "Staging Failed";
            StageButton.IsEnabled = true;
        }

        private async void DeployWar(object sender, RoutedEventArgs e)
        {
            _config.ProfileName = ProfileComboBox.SelectedItem?.ToString() ?? "";
            _config.Platform = PlatformComboBox.SelectedIndex switch
            {
                0 => Platform.RfLambda,
                1 => Platform.RapidRf,
                2 => Platform.MillerMmic,
                _ => Platform.RfLambda
            };

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
    }
}

