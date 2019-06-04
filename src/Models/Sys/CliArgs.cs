using System;
using System.Collections.Generic;
using CommandLine;
using MCTOPP.Models.Algorithm;

namespace MCTOPP.Models.Sys
{
    class CliArgs
    {
        // General options
        [Option('f', "file", Required = true, HelpText = "Input files to be processed")]
        public string InputFile { get; set; }

        [Option("skip-file-log", Default = false, HelpText = "Skip file log for better performance")]
        public bool SkipFileLog { get; set; }

        // Algorithm methods
        [Option('b', "brute-force", Required = false, HelpText = "Use brute force")]
        public bool BruteForce { get; set; }

        [Option('s', "semi-force", Required = false, HelpText = "Use semi brute force")]
        public bool SemiBruteForce { get; set; }

        // SA parameters
        [Option("sa-seed", HelpText = "Initial solution seed")]
        public int InitialSolutionSeed { get; set; }

        [Option("sa-max-iter", HelpText = "Maximum iterations without improvement allowed")]
        public int MaxIterWithoutImprovement { get; set; }

        [Option("sa-cool-fact", HelpText = "Cooling factor")]
        public double CoolingFactor { get; set; }

        [Option("sa-min-swap", HelpText = "Minimum swap tries during shuffle step")]
        public int MinSwapTries { get; set; }

        [Option("sa-max-swap", HelpText = "Maximum swap tries during shuffle step")]
        public int MaxSwapTries { get; set; }

        [Option("sa-max-del", HelpText = "Maximum random delete operations allowed")]
        public int MaxRandomDeleteOperations { get; set; }

        [Option("sa-max-ins", HelpText = "Maximum random insert operations allowed")]
        public int MaxRandomInsertOperations { get; set; }

        [Option("sa-init-sol", HelpText = "Criteria used to generate inital solution, [score, avg_dist]")]
        public string InitialSolutionCriteria { get; set; }

        [Option("sa-cool-func", HelpText = "Cooling function used for reducing the temperature, [geo, lundy]")]
        public string CoolingFunction { get; set; }

        public CoolingFunction CoolingFunctionValue =>
            CoolingFunction == "geo" ?
                Algorithm.CoolingFunction.GeometricalCooling :
                Algorithm.CoolingFunction.LundyCooling;

        public InitialSolutionCriteria InitialSolutionCriteriaValue =>
            InitialSolutionCriteria == "score" ?
                Algorithm.InitialSolutionCriteria.Score :
                Algorithm.InitialSolutionCriteria.AverageDistance;
    }
}