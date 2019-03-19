using System;
using System.Collections.Generic;
using CommandLine;

namespace MCTOPP.Models.Sys
{
    class CliArgs
    {
        [Option('f', "files", Required = true, HelpText = "Input files to be processed")]
        public IEnumerable<string> InputFiles { get; set; }
    }
}