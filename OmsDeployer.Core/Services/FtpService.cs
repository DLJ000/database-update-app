using System;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using OmsDeployer.Core.Models;

namespace OmsDeployer.Core.Services
{
    public class FtpService
    {
        private readonly Logger _logger;

        public FtpService(Logger logger)
        {
            _logger = logger;
        }

        public async Task<bool> UploadWar(DeploymentConfig config, string profileName, IProgress<string> progress)
        {
            try
            {
                var localWarPath = Path.Combine(
                    config.RepoPath, 
                    "lakexy", 
                    "oms", 
                    "target", 
                    $"{profileName}-oms.war"
                );

                if (!File.Exists(localWarPath))
                {
                    _logger.Log($"ERROR: Local WAR file not found: {localWarPath}");
                    progress.Report("ERROR: Local WAR file not found!");
                    return false;
                }

                _logger.Log($"Connecting to FTP server {config.FtpHost}...");
                progress.Report($"Connecting to FTP server...");

                using var client = new FtpClient(config.FtpHost, config.FtpUser, config.FtpPassword);
                await Task.Run(() => client.AutoConnect());

                _logger.Log("Connected. Uploading WAR file...");
                progress.Report("Uploading WAR file...");

                var remotePath = $"{config.FtpUploadPath}/{profileName}-oms.war";
                var result = await Task.Run(() => client.UploadFile(localWarPath, remotePath, FtpRemoteExists.Overwrite, createRemoteDir: true));

                if (result == FtpStatus.Success)
                {
                    _logger.Log($"SUCCESS: Uploaded to {remotePath}");
                    progress.Report("SUCCESS: Upload complete!");
                    return true;
                }
                else
                {
                    _logger.Log($"ERROR: Upload failed with status {result}");
                    progress.Report($"ERROR: Upload failed!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: FTP upload failed: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }
    }
}

