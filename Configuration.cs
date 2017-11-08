using System.Collections.Generic;

namespace RetentionManager
{
    public class Configuration
    {
        public string Source { get; set; }
        public IList<RetentionRule> Rules { get; set; }
    }
}
