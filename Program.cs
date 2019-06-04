using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
using MCTOPP.Helpers;
using MCTOPP.Helpers.Parsers;
using MCTOPP.Models.Algorithm;
using System.IO;
using System.Diagnostics;

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
                var logger = LogFactory.Create(o.SkipFileLog, $"logs{delimiter}{filename}.log");
                string path =
                    $"{Directory.GetCurrentDirectory()}{delimiter}{o.InputFile.TrimStart(delimiter)}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                var meta = MetaData.Create(input);
                var algorithm = o.BruteForce ? "Brute force" : "Simulated Annealing";

                logger.Info($"Input file: {filename}, Algorithm: {algorithm}");

                try
                {
                    var iterations = 0;
                    Solution s = null;
                    Stopwatch stopwatch = Stopwatch.StartNew();

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
                        SimulatedAnnealingAlgorithm alg;
                        alg = !String.IsNullOrWhiteSpace(o.InitialSolutionCriteria) ?
                            new SimulatedAnnealingAlgorithm(
                                o.InitialSolutionSeed,
                                o.MaxIterWithoutImprovement,
                                o.CoolingFactor,
                                o.MinSwapTries,
                                o.MaxSwapTries,
                                o.MaxRandomDeleteOperations,
                                o.MaxRandomInsertOperations,
                                o.InitialSolutionCriteriaValue,
                                o.CoolingFunctionValue
                            ) :
                            new SimulatedAnnealingAlgorithm();

                        logger.Info(alg.PrintParams());
                        logger.Debug("Simulated Annealing solution started");
                        s = alg.Solve(input);
                        logger.Debug("Simulated Annealing solution finished");
                        iterations = alg.TemperatureIterations;
                    }
                    stopwatch.Stop();

                    if (s.IsValid)
                    {
                        logger.Info("Solution");
                        logger.Info(s.PrintSummary() + $" Score: {s.Score}");
                        Console.WriteLine(OutputResult(s, iterations, stopwatch.ElapsedMilliseconds));
                    }
                    else
                    {
                        logger.Error("Solution is not valid!");
                        logger.Error(s.PrintSummary());
                        throw new Exception("Solution is not valid!");
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(NLog.LogLevel.Error, ex);
                    throw new Exception(ex.Message, ex);
                }
            });
        }

        static string OutputResult(Solution s, int iterations, long time)
        {
            return $"{s.Score};{time};{iterations};{s.PrintSummary()}";
        }

    }


}
