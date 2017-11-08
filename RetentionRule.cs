namespace RetentionManager
{
    public class RetentionRule
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public int Stable { get; set; }
        public int Prerelease { get; set; }
    }
}
