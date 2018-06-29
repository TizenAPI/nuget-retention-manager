using System.Collections.Generic;

namespace RetentionManager
{
    public class RetentionRule
    {
        public string Id { get; set; }

        public string Version { get; set; }

        public IList<string> Versions {get; set; }

        public int Stable { get; set; }

        public int Prerelease { get; set; }
    }
}
