using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using MCTOPP.Models.Algorithm;

namespace MCTOPP.Helpers.Parsers
{
    public class FileParser : IDataSetParser
    {
        public ProblemInput ParseInput(string path)
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                throw new Exception("Input path is not a file");

            ProblemInput input = null;
            using (var fs = new StreamReader(path))
            {
                input = new ProblemInput();
                int line = -1;
                string text = "";

                while ((text = fs.ReadLine()) != null)
                {
                    line++;

                    if (line == 0)
                    {
                        var vals = this.ParseTourCount(text);
                        input.TourCount = vals.TourCount;
                        input.PoiCount = vals.PoiCount;
                        input.Budget = vals.Budget;
                        input.Patterns = new List<int[]>(input.TourCount);
                        input.Pois = new List<Poi>(input.PoiCount);
                    }
                    else if (line == 1)
                    {
                        input.MaxPoisOfType = this.ParseMaxPoisOfType(text);
                    }
                    else if (line == 2)
                    {
                        input.PatternLengths = this.ParsePatternLength(text);
                    }
                    else if (line > 2 && line < 3 + input.TourCount)
                    {
                        input.Patterns.Add(this.ParsePattern(text, input.PatternLengths[line - 3]));
                    }
                    else
                    {
                        input.Pois.Add(this.ParsePoi(text));
                    }
                }
            }

            return input;
        }

        private (int TourCount, int PoiCount, int Budget) ParseTourCount(string raw)
        {
            var rawItems = raw.Trim().Split(' ');
            return (
                TourCount: int.Parse(rawItems[0]),
                PoiCount: int.Parse(rawItems[1]),
                Budget: int.Parse(rawItems[2])
            );
        }

        private List<int> ParseMaxPoisOfType(string raw)
        {
            return raw.Trim().Split(' ')
                .Select(x => int.Parse(x))
                .ToList<int>();
        }

        private List<int> ParsePatternLength(string raw)
        {
            return raw.Trim().Split(' ')
                .Select(x => int.Parse(x))
                .ToList<int>();
        }

        private int[] ParsePattern(string raw, int length)
        {
            return raw.Trim().Split(' ')
                .Take(length)
                .Select(x => int.Parse(x))
                .ToArray();
        }

        private Poi ParsePoi(string raw)
        {
            var rawPois = raw.Trim().Split(' ')
                .Select(x => float.Parse(x))
                .ToList<float>();
            var index = rawPois.Skip(8).ToList().FindIndex(x => x == 1);

            return new Poi()
            {
                Id = (int)rawPois[0],
                X = rawPois[1],
                Y = rawPois[2],
                Duration = rawPois[3],
                Score = rawPois[4],
                Open = rawPois[5],
                Close = rawPois[6],
                Cost = rawPois.Count > 7 ? rawPois[7] : 0,
                Type = index,
            };
        }
    }
}