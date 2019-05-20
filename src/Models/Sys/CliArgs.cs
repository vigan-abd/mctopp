using System;
using System.Collections.Generic;
using CommandLine;

namespace MCTOPP.Models.Sys
{
    class CliArgs
    {
        [Option('f', "file", Required = true, HelpText = "Input files to be processed")]
        public string InputFile { get; set; }

        [Option('b', "brute-force", Required = false, HelpText = "Use brute force")]
        public bool BruteForce { get; set; }

        [Option('s', "semi-force", Required = false, HelpText = "Use semi brute force")]
        public bool SemiBruteForce { get; set; }
    }
}