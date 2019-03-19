using System;
using System.Collections.Generic;
using CommandLine;

namespace MCTOPP.Models.Sys
{
    class CliArgs
    {
        [Option('f', "file", Required = true, HelpText = "Input files to be processed")]
        public string InputFile { get; set; }
    }
}