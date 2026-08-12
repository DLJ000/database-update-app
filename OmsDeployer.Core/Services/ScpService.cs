using System;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;
using OmsDeployer.Core.Models;

namespace OmsDeployer.Core.Services
{
    public class ScpService
    {
        private readonly Logger _logger;

        public ScpService(Logger logger)
        {
            _logger = logger;
        }

        public async Task<bool> UploadWar(DeploymentConfig config, string profileName, IProgress<string> progress)
        {
            var localWarPath = Path.Combine(
                config.RepoPath,
                "lakexy",
                "oms",
                "target",
                $"{profileName}-oms.war"
            );

            return await UploadFile(config, localWarPath, $"{profileName}-oms.war", progress);
        }

        public async Task<bool> UploadWarFromPath(DeploymentConfig config, string localWarPath, IProgress<string> progress)
        {
            return await UploadFile(config, localWarPath, Path.GetFileName(localWarPath), progress);
        }

        private async Task<bool> UploadFile(DeploymentConfig config, string localWarPath, string remoteFileName, IProgress<string> progress)
        {
            try
            {
                if (!File.Exists(localWarPath))
                {
                    _logger.Log($"ERROR: Local WAR file not found: {localWarPath}");
                    progress.Report("ERROR: Local WAR file not found!");
                    return false;
                }

                var host = PlatformServer.GetHost(config.Platform);
                _logger.Log($"Connecting to {host} via SCP...");
                progress.Report($"Connecting to {host}...");

                using var client = new ScpClient(host, config.TomcatUser, config.TomcatPassword);
                await Task.Run(() => client.Connect());

                _logger.Log("Connected. Uploading WAR file...");
                progress.Report("Uploading WAR file...");

                var remotePath = $"~/{remoteFileName}";
                using (var fileStream = File.OpenRead(localWarPath))
                {
                    await Task.Run(() => client.Upload(fileStream, remotePath));
                }

                client.Disconnect();

                _logger.Log($"SUCCESS: Uploaded to {host}:{remotePath}");
                progress.Report("SUCCESS: Upload complete!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: SCP upload failed: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }
    }
}
