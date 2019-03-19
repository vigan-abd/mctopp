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
                        input.PointCount = vals.PointCount;
                        input.Budget = vals.Budget;
                        input.Patterns = new List<List<int>>(input.TourCount);
                        input.Points = new List<Point>(input.PointCount);
                    }
                    else if (line == 1)
                    {
                        input.MaxPointsOfType = this.ParseMaxPointsOfType(text);
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
                        input.Points.Add(this.ParsePoint(text));
                    }
                }
            }

            return input;
        }

        private (int TourCount, int PointCount, int Budget) ParseTourCount(string raw)
        {
            var rawItems = raw.Trim().Split(' ');
            return (
                TourCount: int.Parse(rawItems[0]),
                PointCount: int.Parse(rawItems[1]),
                Budget: int.Parse(rawItems[2])
            );
        }

        private List<int> ParseMaxPointsOfType(string raw)
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

        private List<int> ParsePattern(string raw, int length)
        {
            return raw.Trim().Split(' ')
                .Take(length)
                .Select(x => int.Parse(x))
                .ToList<int>();
        }

        private Point ParsePoint(string raw)
        {
            var rawPoints = raw.Trim().Split(' ')
                .Select(x => float.Parse(x))
                .ToList<float>();
            var index = rawPoints.Skip(8).ToList().FindIndex(x => x == 1);

            return new Point()
            {
                Id = (int)rawPoints[0],
                X = rawPoints[1],
                Y = rawPoints[2],
                Duration = rawPoints[3],
                Score = rawPoints[4],
                Open = rawPoints[5],
                Close = rawPoints[6],
                Cost = rawPoints.Count > 7 ? rawPoints[7] : 0,
                Type = index,
            };
        }
    }
}