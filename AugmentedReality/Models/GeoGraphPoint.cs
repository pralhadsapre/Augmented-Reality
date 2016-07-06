using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AugmentedReality.Models
{
    public class GeoGraphPoint : GeoPoint
    {
        public bool IsVisited { get; set; }
        public GeoGraphPoint PreviousPoint { get; set; }
        public double DistanceFromSource { get; set; }
    }
}
