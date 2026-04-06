using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OmsDeployer.Core.Services
{
    public class UiBuildService
    {
        private readonly Logger _logger;

        public UiBuildService(Logger logger)
        {
            _logger = logger;
        }

        public async Task<bool> BuildWar(string uiRepoPath, string profileName, IProgress<string> progress)
        {
            try
            {
                var profilePath = Path.Combine(uiRepoPath, profileName);
                if (!Directory.Exists(profilePath))
                {
                    _logger.Log($"ERROR: Profile folder not found: {profilePath}");
                    progress.Report($"ERROR: Profile folder not found: {profilePath}");
                    return false;
                }

                _logger.Log($"Starting UI build for profile: {profileName}");
                progress.Report($"Starting UI build for profile: {profileName}");
                progress.Report($"Working directory: {profilePath}");

                // Find .bin files inside the bin/ subfolder
                var binFolderPath = Path.Combine(profilePath, "bin");
                if (!Directory.Exists(binFolderPath))
                {
                    _logger.Log($"ERROR: bin folder not found: {binFolderPath}");
                    progress.Report($"ERROR: bin folder not found: {binFolderPath}");
                    return false;
                }

                var binFiles = Directory.GetFiles(binFolderPath, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f)
                    .ToList();

                if (binFiles.Count == 0)
                {
                    _logger.Log($"ERROR: No files found in {binFolderPath}");
                    progress.Report($"ERROR: No files found in {binFolderPath}");
                    return false;
                }

                // Run each .bin file in order
                foreach (var binFile in binFiles)
                {
                    _logger.Log($"Running: {binFile}");
                    progress.Report($"Running: {Path.GetFileName(binFile)}");

                    if (!await RunBinFile(binFile, binFolderPath, progress))
                    {
                        progress.Report($"ERROR: {Path.GetFileName(binFile)} failed");
                        return false;
                    }
                }

                // Verify WAR file exists in target folder
                var targetPath = Path.Combine(profilePath, "target");
                var warFiles = Directory.Exists(targetPath)
                    ? Directory.GetFiles(targetPath, "*.war").ToList()
                    : new List<string>();

                if (warFiles.Count == 0)
                {
                    _logger.Log($"ERROR: No WAR file found in {targetPath}");
                    progress.Report($"ERROR: No WAR file found in target folder!");
                    return false;
                }

                foreach (var war in warFiles)
                {
                    var fileInfo = new FileInfo(war);
                    _logger.Log($"SUCCESS: WAR file created: {war} ({fileInfo.Length:N0} bytes)");
                    progress.Report($"SUCCESS: {Path.GetFileName(war)} ({fileInfo.Length:N0} bytes)");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }

        public string FindWarFile(string uiRepoPath, string profileName)
        {
            var targetPath = Path.Combine(uiRepoPath, profileName, "target");
            if (!Directory.Exists(targetPath))
                return string.Empty;

            return Directory.GetFiles(targetPath, "*.war").FirstOrDefault() ?? string.Empty;
        }

        private async Task<bool> RunBinFile(string binFilePath, string workingDirectory, IProgress<string> progress)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = binFilePath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return false;

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.Log(e.Data);
                        progress.Report(e.Data);
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.Log($"STDERR: {e.Data}");
                        progress.Report($"STDERR: {e.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to run {binFilePath}: {ex.Message}");
                progress.Report($"Failed to run {Path.GetFileName(binFilePath)}: {ex.Message}");
                return false;
            }
        }
    }
}
