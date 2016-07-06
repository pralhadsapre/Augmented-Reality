using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Device.Location;
using AugmentedReality.Models;
using System.Xml;
using System.Xml.Linq;

namespace AugmentedReality.Utilities
{
    public class MapDataHandler
    {
        public static List<GeoPlacemark> FetchPlacemarkData()
        {
            List<GeoPlacemark> placemarks = new List<GeoPlacemark>();
            try
            {
                System.IO.Stream src = Application.GetResourceStream(new Uri("MapData/Points.txt", UriKind.Relative)).Stream;
                string data;
                using (StreamReader sr = new StreamReader(src))
                {
                    while ((data = sr.ReadLine()) != null)
                        placemarks.Add(new GeoPlacemark
                        {
                            Position = new GeoCoordinate(Double.Parse(data.Split(',')[0]), Double.Parse(data.Split(',')[1])),
                            Name = data.Split(',')[2],
                            Orientation = 0
                        });
                }
                return placemarks;
            }
            catch (Exception exp) { return null; }
        }

        public static Dictionary<string, List<GeoPoint>> FetchGraphData()
        {
            Dictionary<string, List<GeoPoint>> graphData = new Dictionary<string, List<GeoPoint>>();
            List<GeoPoint> allPoints = new List<GeoPoint>();
            List<GeoPoint> placemarks = new List<GeoPoint>();
            List<GeoPoint> paths = new List<GeoPoint>();
            XDocument doc;

            GeoPoint tempPoint;

            try
            {
                System.IO.Stream src = Application.GetResourceStream(new Uri("MapData/GraphMap.osm", UriKind.Relative)).Stream;
                string data;
                using (StreamReader sr = new StreamReader(src))
                {
                    doc = XDocument.Parse(sr.ReadToEnd());
                    IEnumerable<XElement> nodes = doc.Descendants("node");
                    foreach (XElement node in nodes)
                    {
                        tempPoint = new GeoPoint
                        {
                            Position = new GeoCoordinate(double.Parse(node.Attribute("lat").Value), double.Parse(node.Attribute("lon").Value)),
                            ID = int.Parse(node.Attribute("id").Value)
                        };

                        if (node.Element("tag") != null)
                        {
                            tempPoint.Name = node.Element("tag").Attribute("v").Value;
                            placemarks.Add(tempPoint);
                        }
                        allPoints.Add(tempPoint);                        
                    }

                    graphData.Add("AllPoints", allPoints);
                    graphData.Add("Placemarks", placemarks);

                    GeoPath tempPath;
                    IEnumerable<XElement> ways = doc.Descendants("way");
                    foreach (XElement way in ways)
                    {
                        tempPath = new GeoPath { Path = new List<GeoPoint>() };
                        foreach (XElement waypoint in way.Descendants("nd"))                        
                            tempPath.Path.Add(new GeoPoint { ID = int.Parse(waypoint.Attribute("ref").Value) });

                        paths.Add(tempPath);
                    }

                    graphData.Add("Paths", paths);

                }
                return graphData;
            }
            catch (Exception exp) { return null; }
        }
    }
}
