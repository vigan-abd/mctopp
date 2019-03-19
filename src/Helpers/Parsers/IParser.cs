using System;
using MCTOPP.Models.Algorithm;

namespace MCTOPP.Helpers.Parsers
{
    public interface IDataSetParser
    {
        ProblemInput ParseInput(string path);
    }
}