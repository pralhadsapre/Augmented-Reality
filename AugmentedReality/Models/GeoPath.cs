using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AugmentedReality.Models
{
    class GeoPath : GeoPoint
    {
        public List<GeoPoint> Path { get; set; }
    }
}
