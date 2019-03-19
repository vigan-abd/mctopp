using System;

namespace MCTOPP.Models.Algorithm
{
    public class Point
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Duration { get; set; }
        public float Score { get; set; }
        public float Open { get; set; }
        public float Close { get; set; }
        public float Cost { get; set; }
        public int Type { get; set; }

        public override string ToString()
        {
            return $"Id: {Id}, X: {X}, Y: {Y}, Duration: {Duration}, Score: {Score}, " +
            $"Open: {Open}, Close: {Close}, Cost: {Cost}, Type: {Type}";
        }
    }
}