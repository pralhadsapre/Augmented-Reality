using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;

namespace AugmentedReality.Utilities
{
    public class LatLongMath
    {
        public static double GetDistance(GeoCoordinate point1, GeoCoordinate point2)
        {
            double R = 6371; // km
            double dLat = LatLongMath.ToRad(point2.Latitude - point1.Latitude);
            double dLon = LatLongMath.ToRad(point2.Longitude - point1.Longitude);
            double lat1 = LatLongMath.ToRad(point1.Latitude);
            double lat2 = LatLongMath.ToRad(point2.Latitude);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;
            return d * 1000;
            //will return distance in metres
        }

        public static double GetBearing(GeoCoordinate position, GeoCoordinate point)
        {
            double dLon = LatLongMath.ToRad(point.Longitude - position.Longitude);
            double lat1 = LatLongMath.ToRad(position.Latitude);
            double lat2 = LatLongMath.ToRad(point.Latitude);
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) -
                    Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            double brng = LatLongMath.ToDeg(Math.Atan2(y, x));
            return brng;
        }

        public static double GetCompassBearing(GeoCoordinate position, GeoCoordinate point)
        {
            return ((GetBearing(position, point) + 360) % 360);
        }

        public static double ToRad(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        public static double ToDeg(double angle)
        {
            return (180 / Math.PI) * angle;
        }        
    }
}
