using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OmsDeployer.Core.Services
{
    public class ProfileScanner
    {
        public static List<string> ScanProfiles(string repoPath)
        {
            var filtersPath = Path.Combine(repoPath, "lakexy", "oms", "src", "main", "filters");
            
            if (!Directory.Exists(filtersPath))
                return new List<string>();

            return Directory.GetFiles(filtersPath, "*.properties")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderBy(p => p)
                .ToList();
        }
    }
}

