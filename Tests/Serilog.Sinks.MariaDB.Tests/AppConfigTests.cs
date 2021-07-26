using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Serilog.Sinks.MariaDB.Tests
{
#if NETFRAMEWORK

    [TestClass]
    public class AppConfigTests
    {
        [TestMethod]
        public void LoadingOfCompleteSection()
        {
            var _logger = new LoggerConfiguration()
                .ReadFrom.AppSettings().CreateLogger();

            // should not throw

            Log.CloseAndFlush();
        }
    }

#endif
}
