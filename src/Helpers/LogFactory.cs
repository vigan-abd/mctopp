using System;
using NLog;

namespace MCTOPP.Helpers
{
    public sealed class LogFactory
    {
        private static Logger instance;
        private LogFactory() { }

        public static Logger Create()
        {
            if (instance == null)
            {
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                var config = new NLog.Config.LoggingConfiguration();

                var logfile = new NLog.Targets.FileTarget("logfile") { FileName = $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}app.log" };
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

                config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

                NLog.LogManager.Configuration = config;

                instance = NLog.LogManager.GetCurrentClassLogger();
            }
            return instance;
        }

    }
}