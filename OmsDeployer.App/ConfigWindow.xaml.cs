using System.Windows;
using OmsDeployer.Core.Models;
using OmsDeployer.Core.Services;

namespace OmsDeployer.App
{
    public partial class ConfigWindow : Window
    {
        private DeploymentConfig _config;
        private CredentialService _credentialService;
        private const string PasswordPlaceholder = "••••••••";
        private bool _ftpPasswordIsPlaceholder = false;
        private bool _rootPasswordIsPlaceholder = false;
        private bool _tomcatPasswordIsPlaceholder = false;

        public ConfigWindow(DeploymentConfig config, CredentialService credentialService)
        {
            InitializeComponent();
            _config = config;
            _credentialService = credentialService;

            FtpHostTextBox.Text = _config.FtpHost;
            FtpUserTextBox.Text = _config.FtpUser;
            SshHostTextBox.Text = _config.SshHost;
            
            // Check if passwords are already saved and show placeholder dots
            UpdatePasswordPlaceholders();
            
            // Clear placeholder when user starts typing
            FtpPasswordBox.PasswordChanged += FtpPasswordBox_PasswordChanged;
            RootPasswordBox.PasswordChanged += RootPasswordBox_PasswordChanged;
            TomcatPasswordBox.PasswordChanged += TomcatPasswordBox_PasswordChanged;
        }
        
        private void UpdatePasswordPlaceholders()
        {
            var (ftpPwd, rootPwd, tomcatPwd) = _credentialService.LoadCredentials();
            
            if (!string.IsNullOrEmpty(ftpPwd))
            {
                FtpPasswordBox.Password = PasswordPlaceholder;
                _ftpPasswordIsPlaceholder = true;
            }
            
            if (!string.IsNullOrEmpty(rootPwd))
            {
                RootPasswordBox.Password = PasswordPlaceholder;
                _rootPasswordIsPlaceholder = true;
            }
            
            if (!string.IsNullOrEmpty(tomcatPwd))
            {
                TomcatPasswordBox.Password = PasswordPlaceholder;
                _tomcatPasswordIsPlaceholder = true;
            }
        }
        
        private void FtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_ftpPasswordIsPlaceholder && FtpPasswordBox.Password != PasswordPlaceholder)
            {
                _ftpPasswordIsPlaceholder = false;
            }
        }
        
        private void RootPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_rootPasswordIsPlaceholder && RootPasswordBox.Password != PasswordPlaceholder)
            {
                _rootPasswordIsPlaceholder = false;
            }
        }
        
        private void TomcatPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_tomcatPasswordIsPlaceholder && TomcatPasswordBox.Password != PasswordPlaceholder)
            {
                _tomcatPasswordIsPlaceholder = false;
            }
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            // Save to config object
            _config.FtpHost = FtpHostTextBox.Text;
            _config.FtpUser = FtpUserTextBox.Text;
            _config.SshHost = SshHostTextBox.Text;
            
            // Get passwords - if placeholder, load from saved credentials; otherwise use what user typed
            var (savedFtpPwd, savedRootPwd, savedTomcatPwd) = _credentialService.LoadCredentials();
            
            string ftpPwd = _ftpPasswordIsPlaceholder || FtpPasswordBox.Password == PasswordPlaceholder 
                ? savedFtpPwd 
                : FtpPasswordBox.Password;
            
            string rootPwd = _rootPasswordIsPlaceholder || RootPasswordBox.Password == PasswordPlaceholder 
                ? savedRootPwd 
                : RootPasswordBox.Password;
            
            string tomcatPwd = _tomcatPasswordIsPlaceholder || TomcatPasswordBox.Password == PasswordPlaceholder 
                ? savedTomcatPwd 
                : TomcatPasswordBox.Password;
            
            // Save passwords (encrypted)
            _credentialService.SaveCredentials(ftpPwd, rootPwd, tomcatPwd);
            
            // Update config object with passwords immediately
            _config.FtpPassword = ftpPwd;
            _config.RootPassword = rootPwd;
            _config.TomcatPassword = tomcatPwd;

            // Save to settings
            Properties.Settings.Default.FtpHost = _config.FtpHost;
            Properties.Settings.Default.SshHost = _config.SshHost;
            Properties.Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void CancelSettings(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

