using System;
using System.Threading.Tasks;
using NuGet.Common;

namespace RetentionManager
{
    public class ConsoleLogger : LoggerBase
    {
        public override void Log(ILogMessage message)
        {
            Console.WriteLine($"[{message.Code}] {message.Message}");
        }

        public override Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.FromResult(0);
        }
    }
}
