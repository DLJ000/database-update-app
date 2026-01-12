using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OmsDeployer.Core.Models;

namespace OmsDeployer.Core.Services
{
    public class BuildService
    {
        private readonly Logger _logger;

        public BuildService(Logger logger)
        {
            _logger = logger;
        }

        public async Task<bool> BuildWar(DeploymentConfig config, IProgress<string> progress)
        {
            try
            {
                _logger.Log("Starting build process...");
                progress.Report("Starting build process...");

                // Step 1: SVN Update
                _logger.Log("Running SVN update...");
                progress.Report("Running SVN update...");
                if (!await RunCommand("svn", "update", config.RepoPath, progress))
                    return false;

                // Step 2: Build product-finder
                var productFinderPath = Path.Combine(config.RepoPath, "lakexy", "product-finder");
                _logger.Log($"Building product-finder at {productFinderPath}...");
                progress.Report("Building product-finder...");
                if (!await RunCommand("mvn", "install", productFinderPath, progress))
                    return false;

                // Step 3: Build omscore
                var omscorePath = Path.Combine(config.RepoPath, "lakexy", "omscore");
                _logger.Log($"Building omscore at {omscorePath}...");
                progress.Report("Building omscore...");
                if (!await RunCommand("mvn", "install", omscorePath, progress))
                    return false;

                // Step 4: Build OMS with profile
                var omsPath = Path.Combine(config.RepoPath, "lakexy", "oms");
                _logger.Log($"Building OMS with profile {config.ProfileName}...");
                progress.Report($"Building OMS with profile {config.ProfileName}...");
                if (!await RunCommand("mvn", $"clean package -P {config.ProfileName}", omsPath, progress))
                    return false;

                // Step 5: Verify WAR file exists
                var warPath = Path.Combine(omsPath, "target", $"{config.ProfileName}-oms.war");
                if (!File.Exists(warPath))
                {
                    _logger.Log($"ERROR: WAR file not found at {warPath}");
                    progress.Report($"ERROR: WAR file not found!");
                    return false;
                }

                var fileInfo = new FileInfo(warPath);
                _logger.Log($"SUCCESS: WAR file created: {warPath} ({fileInfo.Length:N0} bytes)");
                progress.Report($"SUCCESS: WAR file created ({fileInfo.Length:N0} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR: {ex.Message}");
                progress.Report($"ERROR: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RunCommand(string command, string arguments, string workingDirectory, IProgress<string> progress)
        {
            try
            {
                // On Windows, find the full path to the command
                string commandPath = FindCommandPath(command);
                if (string.IsNullOrEmpty(commandPath))
                {
                    _logger.Log($"ERROR: Command '{command}' not found in PATH");
                    progress.Report($"ERROR: Command '{command}' not found in PATH");
                    return false;
                }

                // On Windows, if the command is a .cmd or .bat file, we need to run it via cmd.exe
                bool isWindowsBatchFile = Environment.OSVersion.Platform == PlatformID.Win32NT && 
                                           (commandPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || 
                                            commandPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
                
                string fileName;
                string fullArguments;
                
                if (isWindowsBatchFile)
                {
                    // Run batch file through cmd.exe
                    fileName = "cmd.exe";
                    fullArguments = $"/c \"{commandPath}\" {arguments}";
                }
                else
                {
                    fileName = commandPath;
                    fullArguments = arguments;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = fullArguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return false;

                // Capture output
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
                        _logger.Log($"ERROR: {e.Data}");
                        progress.Report($"ERROR: {e.Data}");
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to run {command}: {ex.Message}");
                progress.Report($"Failed to run {command}: {ex.Message}");
                return false;
            }
        }

        private string FindCommandPath(string command)
        {
            // On Windows, try to find the command using where.exe
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try
                {
                    var whereProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "where.exe",
                            Arguments = command,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    whereProcess.Start();
                    var output = whereProcess.StandardOutput.ReadToEnd();
                    whereProcess.WaitForExit();

                    if (whereProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        // Get all paths from the output
                        var paths = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToList();

                        if (paths.Count > 0)
                        {
                            // Prefer .cmd files on Windows
                            var cmdPath = paths.FirstOrDefault(p => p.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase));
                            if (cmdPath != null)
                                return cmdPath;
                            
                            // Otherwise return the first path
                            return paths[0];
                        }
                    }
                }
                catch
                {
                    // If where.exe fails, fall through to PATH search
                }
            }

            // Fallback: return the command as-is
            return command;
        }
    }
}

