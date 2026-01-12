using System.Windows;
using OmsDeployer.Core.Models;
using OmsDeployer.Core.Services;

namespace OmsDeployer.App
{
    public partial class ConfigWindow : Window
    {
        private DeploymentConfig _config;
        private CredentialService _credentialService;

        public ConfigWindow(DeploymentConfig config, CredentialService credentialService)
        {
            InitializeComponent();
            _config = config;
            _credentialService = credentialService;

            FtpHostTextBox.Text = _config.FtpHost;
            FtpUserTextBox.Text = _config.FtpUser;
            SshHostTextBox.Text = _config.SshHost;
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            _config.FtpHost = FtpHostTextBox.Text;
            _config.FtpUser = FtpUserTextBox.Text;
            _config.SshHost = SshHostTextBox.Text;

            _credentialService.SaveCredentials(
                FtpPasswordBox.Password,
                RootPasswordBox.Password,
                TomcatPasswordBox.Password
            );

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

