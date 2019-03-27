using System;
using System.Collections.Generic;

namespace MCTOPP.Models.Algorithm
{
    public class Solution
    {
        public HashSet<int>[] tours { get; set; }
        public List<float>[] times { get; set; } // travel from prev + duration
    }
}