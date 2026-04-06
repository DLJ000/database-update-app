using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OmsDeployer.Core.Services
{
    public static class UiProfileScanner
    {
        public static List<string> ScanProfiles(string uiRepoPath)
        {
            if (string.IsNullOrEmpty(uiRepoPath) || !Directory.Exists(uiRepoPath))
                return new List<string>();

            return Directory.GetDirectories(uiRepoPath)
                .Select(d => Path.GetFileName(d))
                .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("."))
                .OrderBy(n => n)
                .ToList();
        }
    }
}
