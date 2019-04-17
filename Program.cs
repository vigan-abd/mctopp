using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
using MCTOPP.Helpers;
using MCTOPP.Helpers.Parsers;
using MCTOPP.Models.Algorithm;

namespace MCTOPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliArgs>(args)
            .WithParsed<CliArgs>(o =>
            {
                var logger = LogFactory.Create();
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                string path =
                    $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}{o.InputFile.TrimStart(delimiter)}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                var meta = MetaData.Create(input);

                // var solution = new Solution(2, meta);

                // var pois = new int[] { 3, 6, 9, 5 };
                // var types = new int[] { 3, 2, 4, 8 };
                // for (int i = 0; i < pois.Length; i++)
                // {
                //     var poi = input.Pois[pois[i]];
                //     var res = solution.Insert(poi.Id, types[i], i, 0);
                // }

                // var _pois = new int[] { 2, 4, 14 };
                // var _types = new int[] { 1, 5, 9 };
                // for (int i = 0; i < _pois.Length; i++)
                // {
                //     var poi = input.Pois[_pois[i]];
                //     var res = solution.Insert(poi.Id, _types[i], i, 1);
                // }
                // // var testPoi = input.Pois[10];
                // // var _res = solution.Insert(testPoi.Id, 3, 2, 0);
                // // var _res = solution.Remove(0, 0);
                // // var _res = solution.Swap(5, 5, 1, 0);
                // var _res = solution.ArePoisUnique();
                // _res = solution.IsPatternValid();
                // logger.Log(NLog.LogLevel.Debug, _res ? "Solution is valid" : "Solution is not valid");
                // logger.Log(NLog.LogLevel.Debug, solution.PrintSummary());

                if (o.BruteForce)
                {
                    try
                    {
                        var alg = new BruteForceAlgorithm();
                        logger.Log(NLog.LogLevel.Debug, "Brute force solution started");
                        var s = alg.Solve(input);
                        logger.Log(NLog.LogLevel.Debug, "Brute force solution finished");
                        if (s.IsValid)
                        {
                            logger.Log(NLog.LogLevel.Info, "Solution");
                            logger.Log(NLog.LogLevel.Info, s.PrintSummary());
                        }
                        else
                        {
                            logger.Log(NLog.LogLevel.Error, "Solution is not valid!");
                            logger.Log(NLog.LogLevel.Error, s.PrintSummary());
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Log(NLog.LogLevel.Error, ex);
                    }
                }
                else
                {
                    var alg = new SimulatedAnnealingAlgorithm();
                    logger.Log(NLog.LogLevel.Debug, "Simulated Annealing solution started");
                    var s = alg.Solve(input);
                    logger.Log(NLog.LogLevel.Debug, "Simulated Annealing solution finished");
                    if (s.IsValid)
                    {
                        logger.Log(NLog.LogLevel.Info, "Solution");
                        logger.Log(NLog.LogLevel.Info, s.PrintSummary());
                    }
                    else
                    {
                        logger.Log(NLog.LogLevel.Error, "Solution is not valid!");
                        logger.Log(NLog.LogLevel.Error, s.PrintSummary());
                    }
                }
            });
        }



    }


}
