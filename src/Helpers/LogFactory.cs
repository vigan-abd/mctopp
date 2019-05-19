using System;
using NLog;

namespace MCTOPP.Helpers
{
    public sealed class LogFactory
    {
        private static Logger instance;
        private LogFactory() { }

        public static Logger Create(string filename = "app.log")
        {
            if (instance == null)
            {
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                var config = new NLog.Config.LoggingConfiguration();
                var layout = "${longdate} | ${level:uppercase=true} | ${message}";;

                var logfile = new NLog.Targets.FileTarget("logfile") { FileName = $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}{filename}" };
                logfile.Layout = layout;
                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                logconsole.Layout = layout;

                config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

                NLog.LogManager.Configuration = config;

                instance = NLog.LogManager.GetCurrentClassLogger();
            }
            return instance;
        }

    }
}