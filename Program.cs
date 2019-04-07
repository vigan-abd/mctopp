using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
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
                var delimiter = Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX ? '/' : '\\';
                string path =
                    $"{System.IO.Directory.GetCurrentDirectory()}{delimiter}{o.InputFile.TrimStart(delimiter)}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                var meta = MetaData.Create(input);

                var solution = new Solution(1, meta);

                var pois = new int[] { 3, 6, 9, 5 };
                var types = new int[] { 3, 2, 4, 8 };

                for (int i = 0; i < pois.Length; i++)
                {
                    var poi = input.Pois[pois[i]];
                    var res = solution.Insert(poi.Id, types[i], i, 0);
                }
                // var testPoi = input.Pois[10];
                // var _res = solution.Insert(testPoi.Id, 3, 2, 0);
                // var _res = solution.Remove(0, 0);
                var _res = solution.Swap(5, 5, 1, 0);
                // var _res = solution.IsPatternValid();
                // _res = solution.ArePoisUnique();

                // solutionPois[1].Add(input.Pois[2]);
                // solutionPois[1].Add(input.Pois[4]);
                // solutionPois[1].Add(input.Pois[14]);

                Console.WriteLine(input.ToString());
            });
        }
    }
}
