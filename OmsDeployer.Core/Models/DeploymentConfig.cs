namespace OmsDeployer.Core.Models
{
    public class DeploymentConfig
    {
        public string RepoPath { get; set; } = string.Empty;
        public string UiRepoPath { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public Platform Platform { get; set; } = Platform.RfLambda;
        public string TomcatUser { get; set; } = "tomcat";
        public string TomcatPassword { get; set; } = string.Empty;
    }

    public enum Platform
    {
        RfLambda,      // ""
        RapidRf,       // ".rapid"
        MillerMmic,    // ".millermmic"
        DBWave_Tomcat9 // ".dbwave"
    }

    public static class PlatformServer
    {
        public static string GetHost(Platform platform) => platform switch
        {
            Platform.RfLambda => "rflambda.com",
            Platform.RapidRf => "rapidrf.com",
            Platform.MillerMmic => "millermmic.com",
            Platform.DBWave_Tomcat9 => "dbwave.com",
            _ => "rflambda.com"
        };
    }
}
