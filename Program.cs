using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;
using MCTOPP.Helpers.Parsers;

namespace MCTOPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliArgs>(args)
            .WithParsed<CliArgs>(o =>
            {
                string path = $"{System.IO.Directory.GetCurrentDirectory()}/{o.InputFile.TrimStart('/')}";
                IDataSetParser parser = new FileParser();
                var input = parser.ParseInput(path);
                Console.WriteLine(input);
            });
        }
    }
}
