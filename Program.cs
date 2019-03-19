using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Sys;

namespace MCTOPP
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliArgs>(args)
            .WithParsed<CliArgs>(o =>
            {
                Console.WriteLine($"{String.Join(',', o.InputFiles)}");
                Console.WriteLine("Hello World!");
            });
        }
    }
}
