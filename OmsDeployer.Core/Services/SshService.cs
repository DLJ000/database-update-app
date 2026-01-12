using System;
using System.Threading.Tasks;
using Renci.SshNet;
using OmsDeployer.Core.Models;

namespace OmsDeployer.Core.Services
{
    public class SshService
    {
        private readonly Logger _logger;

        public SshService(Logger logger)
        {
            _logger = logger;
        }

        public async Task<bool> StageToTomcat(DeploymentConfig config, string profileName, IProgress<string> progress)
        {
            try
            {
                _logger.Log($"Connecting to SSH server {config.SshHost} as root...");
                progress.Report("Connecting to SSH server...");

                using var client = new SshClient(config.SshHost, config.RootUser, config.RootPassword);
                client.Connect();

                var sourcePath = $"{config.FtpUploadPath}/{profileName}-oms.war";
                var destPath = $"{config.TomcatPath}/{profileName}-oms.war";

                _logger.Log($"Moving {sourcePath} to {destPath}...");
                progress.Report("Moving WAR file to tomcat folder...");

                var command = client.CreateCommand($"mv {sourcePath} {destPath}");
                var result = await Task.Run(() => command.Execute());

                if (command.ExitStatus == 0)
                {
                    _logger.Log($"SUCCESS: File staged to {destPath}");
                    progress.Report("SUCCESS: File staged!");
                    return true;
                }
                else
                {
                    _logger.Log($"ERROR: {command.Error}");
                    progress.Report($"ERROR: {command.Error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: SSH staging failed: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Deploy(DeploymentConfig config, string profileName, IProgress<string> progress)
        {
            try
            {
                _logger.Log($"Connecting to SSH server {config.SshHost} as tomcat...");
                progress.Report("Connecting to SSH server...");

                using var client = new SshClient(config.SshHost, config.TomcatUser, config.TomcatPassword);
                client.Connect();

                var platformSuffix = config.Platform switch
                {
                    Platform.RfLambda => "",
                    Platform.RapidRf => ".rapid",
                    Platform.MillerMmic => ".millermmic",
                    _ => ""
                };

                var date = DateTime.Now.ToString("yyyyMMdd");
                var currentWar = $"oms/oms{platformSuffix}.war";
                var backupWar = $"oms/oms{platformSuffix}.war.{date}";
                var sourceWar = $"{config.TomcatPath}/{profileName}-oms.war";
                var targetWar = $"oms/oms{platformSuffix}.war";

                _logger.Log($"Creating backup: {backupWar}...");
                progress.Report("Creating backup...");

                // Backup existing WAR
                var backupCmd = client.CreateCommand($"mv {currentWar} {backupWar}");
                await Task.Run(() => backupCmd.Execute());
                // Ignore error if file doesn't exist (first deployment)

                _logger.Log($"Copying {sourceWar} to {targetWar}...");
                progress.Report("Deploying new WAR file...");

                var copyCmd = client.CreateCommand($"cp {sourceWar} {targetWar}");
                await Task.Run(() => copyCmd.Execute());

                if (copyCmd.ExitStatus != 0)
                {
                    _logger.Log($"ERROR: {copyCmd.Error}");
                    progress.Report($"ERROR: {copyCmd.Error}");
                    return false;
                }

                // If RfLambda, also copy to webapps
                if (config.Platform == Platform.RfLambda)
                {
                    _logger.Log("Copying to webapps/oms.war (RfLambda)...");
                    progress.Report("Copying to webapps...");

                    var webappsCmd = client.CreateCommand($"cp oms/oms.war webapps/oms.war");
                    await Task.Run(() => webappsCmd.Execute());
                }

                // Clean up staged file
                _logger.Log("Cleaning up staged file...");
                var cleanupCmd = client.CreateCommand($"rm {sourceWar}");
                await Task.Run(() => cleanupCmd.Execute());

                _logger.Log("SUCCESS: Deployment complete!");
                progress.Report("SUCCESS: Deployment complete!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: Deployment failed: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }
    }
}

