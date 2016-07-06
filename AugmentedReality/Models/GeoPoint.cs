using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;

namespace AugmentedReality.Models
{
    public class GeoPoint
    {        
        public int ID { get; set; }
        public String Name { get; set; }
        public GeoCoordinate Position { get; set; }               
    }
}
