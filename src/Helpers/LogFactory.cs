using System;
using NLog;

namespace MCTOPP.Helpers
{
    public sealed class LogFactory
    {
        private static Logger instance;
        private LogFactory() { }

        public static Logger Create(bool skipFileLog = false, string filename = "app.log")
        {
            if (instance == null)
            {
                var config = new NLog.Config.LoggingConfiguration();
                var layout = "${longdate} | ${level:uppercase=true} | ${message}"; ;

                if (!skipFileLog)
                {
                    var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                        Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                    var logfile = new NLog.Targets.FileTarget("logfile") { FileName = $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}{filename}" };
                    logfile.Layout = layout;
                    config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
                }

                var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                logconsole.Layout = layout;
                config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);

                NLog.LogManager.Configuration = config;

                instance = NLog.LogManager.GetCurrentClassLogger();
            }
            return instance;
        }

    }
}