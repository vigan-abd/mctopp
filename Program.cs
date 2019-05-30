using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
using MCTOPP.Helpers;
using MCTOPP.Helpers.Parsers;
using MCTOPP.Models.Algorithm;
using System.IO;

namespace MCTOPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliArgs>(args)
            .WithParsed<CliArgs>(o =>
            {
                var filename = Path.GetFileName(o.InputFile);
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                var logger = LogFactory.Create($"logs{delimiter}{filename}.log");
                string path =
                    $"{Directory.GetCurrentDirectory()}{delimiter}{o.InputFile.TrimStart(delimiter)}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                var meta = MetaData.Create(input);
                var algorithm = o.BruteForce ? "Brute force" : "Simulated Annealing";

                logger.Info($"Input file: {filename}, Algorithm: {algorithm}");

                try
                {
                    Solution s = null;

                    if (o.BruteForce)
                    {
                        var alg = new BruteForceAlgorithm();
                        logger.Debug("Brute force solution started");
                        s = alg.Solve(input);
                        logger.Debug("Brute force solution finished");
                    }
                    else if (o.SemiBruteForce)
                    {
                        var alg = new CleverBruteForceAlgorithm();
                        logger.Debug("Semi brute force solution started");
                        s = alg.Solve(input);
                        logger.Debug("Semi brute force solution finished");
                    }
                    else
                    {
                        var alg = new SimulatedAnnealingAlgorithm();
                        logger.Info(alg.PrintParams());
                        logger.Debug("Simulated Annealing solution started");
                        s = alg.Solve(input);
                        logger.Debug("Simulated Annealing solution finished");
                    }

                    if (s.IsValid)
                    {
                        logger.Info("Solution");
                        logger.Info(s.PrintSummary());
                    }
                    else
                    {
                        logger.Error("Solution is not valid!");
                        logger.Error(s.PrintSummary());
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(NLog.LogLevel.Error, ex);
                }
            });
        }



    }


}
