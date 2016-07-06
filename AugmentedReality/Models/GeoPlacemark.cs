using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;

namespace AugmentedReality.Models
{
    public class GeoPlacemark : GeoPoint
    {
        public double Orientation { get; set; }
        public double Distance { get; set; }
        public double Angle { get; set; }
        public double DisplayX { get; set; }
        public double DisplayY { get; set; }

        public string AdjustedName
        {
            get
            {
                if (Name.Length > 12)
                    return Name.Substring(0, 12) + " ...";
                else
                    return Name;
            }
        }

        public GeoPlacemark()
        { }

        public GeoPlacemark(GeoPoint point)
        {
            this.ID = point.ID;
            this.Name = point.Name;
            this.Position = point.Position;
        }

        public GeoPlacemark(GeoGraphPoint point)
        {
            this.ID = point.ID;
            this.Name = point.Name;
            this.Position = point.Position;
        }
    }
}
