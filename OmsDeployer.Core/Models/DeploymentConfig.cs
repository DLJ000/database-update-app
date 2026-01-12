namespace OmsDeployer.Core.Models
{
    public class DeploymentConfig
    {
        public string RepoPath { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public Platform Platform { get; set; } = Platform.RfLambda;
        public string FtpHost { get; set; } = "ftp.rflambda.com";
        public string FtpUser { get; set; } = "ftpuser";
        public string FtpPassword { get; set; } = string.Empty;
        public string SshHost { get; set; } = string.Empty;
        public string RootUser { get; set; } = "root";
        public string RootPassword { get; set; } = string.Empty;
        public string TomcatUser { get; set; } = "tomcat";
        public string TomcatPassword { get; set; } = string.Empty;
        public string TomcatPath { get; set; } = "/opt/tomcat7";
        public string FtpUploadPath { get; set; } = "/var/www/webadmin/data/ftpuser";
    }

    public enum Platform
    {
        RfLambda,      // ""
        RapidRf,       // ".rapid"
        MillerMmic     // ".millermmic"
    }
}

