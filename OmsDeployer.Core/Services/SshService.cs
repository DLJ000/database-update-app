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

        public async Task<bool> DeployUi(DeploymentConfig config, string warFileName, IProgress<string> progress)
        {
            try
            {
                var host = PlatformServer.GetHost(config.Platform);
                _logger.Log($"Connecting to SSH server {host} as {config.TomcatUser}...");
                progress.Report("Connecting to SSH server...");

                using var client = new SshClient(host, config.TomcatUser, config.TomcatPassword);
                client.Connect();

                var date = DateTime.Now.ToString("yyyyMMdd");
                var webappsRoot = "~/webapps/ROOT.war";
                var backup = $"~/oms/ROOT.war.{date}";
                var staged = $"~/{warFileName}";
                var shutdown = "~/shutdown.sh";
                var startup = "~/startup.sh";

                // Step 1: Backup ROOT.war
                _logger.Log($"Backing up {webappsRoot} to {backup}...");
                progress.Report("Backing up ROOT.war...");
                var backupCmd = client.CreateCommand($"cp {webappsRoot} {backup}");
                await Task.Run(() => backupCmd.Execute());
                if (backupCmd.ExitStatus != 0)
                    progress.Report($"WARNING: Backup skipped: {backupCmd.Error}");
                else
                    progress.Report($"Backed up to {backup}");

                // Step 2: Shutdown Tomcat
                _logger.Log("Shutting down Tomcat...");
                progress.Report("Shutting down Tomcat...");
                var shutdownCmd = client.CreateCommand(shutdown);
                await Task.Run(() => shutdownCmd.Execute());

                // Step 3: Move staged WAR to webapps/ROOT.war
                _logger.Log($"Moving {staged} to {webappsRoot}...");
                progress.Report("Deploying new WAR...");
                var moveCmd = client.CreateCommand($"mv {staged} {webappsRoot}");
                await Task.Run(() => moveCmd.Execute());
                if (moveCmd.ExitStatus != 0)
                {
                    _logger.Log($"ERROR: {moveCmd.Error}");
                    progress.Report($"ERROR: {moveCmd.Error}");
                    return false;
                }

                // Step 4: Start Tomcat
                _logger.Log("Starting Tomcat...");
                progress.Report("Starting Tomcat...");
                var startupCmd = client.CreateCommand(startup);
                await Task.Run(() => startupCmd.Execute());

                _logger.Log("SUCCESS: UI deployment complete!");
                progress.Report("SUCCESS: UI deployment complete!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: UI deployment failed: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Deploy(DeploymentConfig config, string profileName, IProgress<string> progress)
        {
            try
            {
                var host = PlatformServer.GetHost(config.Platform);
                _logger.Log($"Connecting to SSH server {host} as tomcat...");
                progress.Report("Connecting to SSH server...");

                using var client = new SshClient(host, config.TomcatUser, config.TomcatPassword);
                client.Connect();

                var date = DateTime.Now.ToString("yyyyMMdd");
                var omsWar = "~/oms/oms.war";
                var backupWar = $"~/oms/oms.war.{date}";
                var sourceWar = $"~/{profileName}-oms.war";
                var webappsWar = "~/webapps/oms.war";

                // Backup existing oms/oms.war
                _logger.Log($"Creating backup: {backupWar}...");
                progress.Report("Creating backup...");
                var backupCmd = client.CreateCommand($"mv {omsWar} {backupWar}");
                await Task.Run(() => backupCmd.Execute());
                // Ignore error if file doesn't exist (first deployment)

                // Copy staged WAR to oms/oms.war
                _logger.Log($"Copying {sourceWar} to {omsWar}...");
                progress.Report("Deploying to oms/oms.war...");
                var copyCmd = client.CreateCommand($"cp {sourceWar} {omsWar}");
                await Task.Run(() => copyCmd.Execute());

                if (copyCmd.ExitStatus != 0)
                {
                    _logger.Log($"ERROR: {copyCmd.Error}");
                    progress.Report($"ERROR: {copyCmd.Error}");
                    return false;
                }

                // Remove then copy to webapps/oms.war (overwrite not allowed)
                _logger.Log($"Removing {webappsWar}...");
                progress.Report("Removing old webapps/oms.war...");
                var rmCmd = client.CreateCommand($"rm -f {webappsWar}");
                await Task.Run(() => rmCmd.Execute());

                _logger.Log($"Copying to {webappsWar}...");
                progress.Report("Copying to webapps/oms.war...");
                var webappsCmd = client.CreateCommand($"cp {omsWar} {webappsWar}");
                await Task.Run(() => webappsCmd.Execute());

                if (webappsCmd.ExitStatus != 0)
                {
                    _logger.Log($"ERROR copying to webapps: {webappsCmd.Error}");
                    progress.Report($"ERROR copying to webapps: {webappsCmd.Error}");
                    return false;
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

