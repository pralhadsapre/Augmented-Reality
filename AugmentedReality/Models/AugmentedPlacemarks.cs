using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using AugmentedReality.Utilities;
using AugmentedReality.Models;

namespace AugmentedReality.Models
{
    public class AugmentedPlacemarks
    {
        public enum AugmentedMode
        {
            Explorer,
            Navigator
        };

        private List<GeoPlacemark> allPlacemarks;
        private List<GeoPlacemark> visiblePlacemarks;
        private double displayWidth, displayHeight;

        private double angularSpan = 60;  //The span degrees on either side
        private double maxDistance = 200;
        private double left, right;
        private double multiplierX = 3.0, multiplierY = 3.5, offset = 100;
        private double distanceThreshold = 20.0;  //The distance within which if a user comes, the next point is considered

        private Dictionary<string, List<GeoPoint>> mapData;        
        GeoCoordinate currentPosition;
        
        private Dictionary<int, int> pointsIndex;
        private double[,] distanceMatrix;
        private Dictionary<int, GeoGraphPoint> pointsByID;

        private List<GeoGraphPoint> unvisitedNodes;
        private List<GeoPoint> allPoints;

        private List<GeoPlacemark> pathWay;

        public AugmentedMode Mode { get; set; }

        public AugmentedPlacemarks()
        {
            Mode = AugmentedMode.Explorer;
            mapData = MapDataHandler.FetchGraphData();
            allPlacemarks = new List<GeoPlacemark>();

            foreach (GeoPoint point in mapData["Placemarks"])
                allPlacemarks.Add(new GeoPlacemark(point));
                        
            allPoints = mapData["AllPoints"];

            pointsIndex = new Dictionary<int, int>();
            pointsByID = new Dictionary<int, GeoGraphPoint>();

            int index = 0;
            foreach (GeoPoint point in mapData["AllPoints"])
                pointsIndex.Add(point.ID, index++);

            foreach (GeoPoint point in allPoints)
                pointsByID.Add(point.ID, new GeoGraphPoint { ID = point.ID, Name = point.Name, Position = point.Position });
            
            currentPosition = new GeoCoordinate();

            PrepareDistanceMatrix();
            //FindShortestPath(pointsByID[-1460], pointsByID[-1508]);
        }

        #region Graph Algorithms

        private void PrepareDistanceMatrix()
        {
            distanceMatrix = new double[allPoints.Count, allPoints.Count];
            for (int i = 0; i < allPoints.Count; i++)
                for (int j = 0; j < allPoints.Count; j++)
                    distanceMatrix[i, j] = -1000;

            foreach (GeoPath path in mapData["Paths"])
            {
                GeoPoint point1 = path.Path.ElementAt(0);
                GeoPoint point2;
                for (int i = 1; i < path.Path.Count; i++)
                {
                    point2 = path.Path.ElementAt(i);
                    double dist = LatLongMath.GetDistance(pointsByID[point1.ID].Position, pointsByID[point2.ID].Position);

                    //Important since the path is both ways - bidirectional and the probe may occur from any side.
                    distanceMatrix[pointsIndex[point1.ID], pointsIndex[point2.ID]] = dist;
                    distanceMatrix[pointsIndex[point2.ID], pointsIndex[point1.ID]] = dist;

                    //The right sequence is to make the point1 = point2 otherwise the base always remains the same
                    point1 = point2;
                }
            }

            unvisitedNodes = new List<GeoGraphPoint>(allPoints.Count);
        }

        private List<GeoPlacemark> FindShortestPath(GeoPoint source, GeoPoint destination)
        {
            List<GeoPlacemark> route = new List<GeoPlacemark>();

            foreach (GeoGraphPoint point in pointsByID.Values)
            {
                point.IsVisited = false;
                point.PreviousPoint = null;
                point.DistanceFromSource = double.PositiveInfinity;
            }

            GeoGraphPoint sourcePoint = pointsByID[source.ID];
            sourcePoint.DistanceFromSource = 0;
            sourcePoint.PreviousPoint = null;

            unvisitedNodes.Add(sourcePoint);

            GeoGraphPoint currentPoint, neighbour;
            double dist;

            while (unvisitedNodes.Count != 0)
            {
                currentPoint = unvisitedNodes.ElementAt(0);

                for (int i = 0; i < allPoints.Count; i++)
                {                    
                    neighbour = pointsByID.ElementAt(i).Value;
                    if (!neighbour.IsVisited)
                    {
                        if ((dist = distanceMatrix[pointsIndex[currentPoint.ID], i]) > 0)
                            if (currentPoint.DistanceFromSource + dist < neighbour.DistanceFromSource)
                            {
                                neighbour.DistanceFromSource = currentPoint.DistanceFromSource + dist;
                                neighbour.PreviousPoint = currentPoint;

                                int count = unvisitedNodes.Count, j;
                                for (j = 0; j < count; j++)
                                    if (unvisitedNodes.ElementAt(j).DistanceFromSource >= neighbour.DistanceFromSource)
                                    {
                                        unvisitedNodes.Insert(j, neighbour);
                                        break;
                                    }
                                if (j == count)
                                    unvisitedNodes.Insert(j, neighbour);                                
                            }
                    }
                }

                currentPoint.IsVisited = true;
                unvisitedNodes.Remove(currentPoint);
            }

            currentPoint = pointsByID[destination.ID];
            while (currentPoint != null)
            {
                route.Insert(0, new GeoPlacemark(currentPoint));
                currentPoint = currentPoint.PreviousPoint;
            }
            return route;
        }

        #endregion

        public void SetDisplaySize(double width, double height)
        {
            displayWidth = width;
            displayHeight = height;
        }

        public void PositionUpdate(GeoCoordinate position)
        {
            currentPosition = position;
            if (Mode == AugmentedMode.Explorer)            
                UpdatePlacemarks();            
            else if (Mode == AugmentedMode.Navigator)
            {
                if (pathWay != null)
                {
                    if (pathWay.Count > 0)                    
                        if (LatLongMath.GetDistance(position, pathWay.ElementAt(0).Position) <= distanceThreshold)
                            pathWay.RemoveAt(0);
                    
                    if (pathWay.Count > 0)
                    {
                        GeoPlacemark place = pathWay.ElementAt(0);
                        place.Orientation = LatLongMath.GetCompassBearing(position, place.Position);
                        place.Distance = LatLongMath.GetDistance(position, place.Position);
                    }
                }
            }
        }

        private void UpdatePlacemarks()
        {
            foreach (GeoPlacemark placemark in allPlacemarks)
            {
                placemark.Orientation = LatLongMath.GetCompassBearing(currentPosition, placemark.Position);
                placemark.Distance = LatLongMath.GetDistance(currentPosition, placemark.Position);
            }
        }

        public GeoPlacemark GetDummyNextPoint()
        {
            if (pathWay.Count > 0)
                return pathWay.ElementAt(0);
            else
                return null;            
        }

        public List<GeoPlacemark> GetDummyPathWay()
        {
            return pathWay;
        }

        public GeoPlacemark GetNextPoint(double compassHeading)
        {
            if (pathWay.Count > 0)
            {
                GeoPlacemark place = pathWay.ElementAt(0);

                double newSpan = 80;

                left = ((compassHeading - newSpan) + 360) % 360;
                right = ((compassHeading + newSpan) + 360) % 360;

                place.Angle = FindAngularDisplacement(place, compassHeading);

                if (place.Angle != -180)
                {
                    double mathAngle = 90 - Math.Abs(place.Angle);
                    double x = (place.Distance * Math.Cos(LatLongMath.ToRad(mathAngle)));
                    double y = (place.Distance * Math.Sin(LatLongMath.ToRad(mathAngle)));

                    x = (x * ((displayWidth / 3) / place.Distance));
                    y = (place.Distance * (displayHeight / (maxDistance / 3)));

                    if (place.Angle < 0)
                        x = -x;

                    place.DisplayX = (displayWidth / 2) + x;
                    //place.DisplayY = (displayHeight - y);
                    place.DisplayY = (displayHeight) - y;
                }

                return place;
            }
            return null;
        }

        public void SwitchToNavigator(GeoPlacemark destination)
        {            
            int nodeID = int.MaxValue;
            double tempDistance = double.PositiveInfinity, dummy;
            foreach (GeoPoint point in allPoints)            
                if ((dummy = LatLongMath.GetDistance(currentPosition, point.Position)) < tempDistance)
                {
                    tempDistance = dummy;
                    nodeID = point.ID;
                }

            if (nodeID != int.MaxValue)            
                pathWay = FindShortestPath(pointsByID[nodeID], destination);

            GeoPlacemark place = pathWay.ElementAt(0);
            place.Distance = LatLongMath.GetDistance(currentPosition, place.Position);
            place.Orientation = LatLongMath.GetCompassBearing(currentPosition, place.Position);

            Mode = AugmentedMode.Navigator;
        }

        public void SwitchToExplorer()
        {
            UpdatePlacemarks();
            pathWay = null;
            Mode = AugmentedMode.Explorer;
        }

        public List<GeoPlacemark> VisiblePlacemarks(double compassHeading)
        {
            PrepareVisiblePlacemarks(compassHeading);
            PrepareDisplayPositions();
            return visiblePlacemarks;
        }

        private void PrepareDisplayPositions()
        {
            foreach (GeoPlacemark place in visiblePlacemarks)
            {
                double mathAngle = 90 - Math.Abs(place.Angle);
                double x = (place.Distance * Math.Cos(LatLongMath.ToRad(mathAngle)));
                double y = (place.Distance * Math.Sin(LatLongMath.ToRad(mathAngle)));

                x = (x * (displayWidth / maxDistance));
                y = (place.Distance * (displayHeight / maxDistance));

                if (place.Angle < 0)
                    x = -x;

                place.DisplayX = (displayWidth / 2) + x;
                place.DisplayY = (displayHeight - y);
            }            
        }
        
        private void PrepareVisiblePlacemarks(double compassHeading)
        {
            visiblePlacemarks = new List<GeoPlacemark>();

            left = ((compassHeading - angularSpan) + 360) % 360;
            right = ((compassHeading + angularSpan) + 360) % 360;

            double angularDisplacement;

            foreach (GeoPlacemark place in allPlacemarks)
                if ((angularDisplacement = FindAngularDisplacement(place, compassHeading)) != -180)
                {
                    place.Angle = angularDisplacement;                    
                    visiblePlacemarks.Add(place);
                }            
        }

        private double FindAngularDisplacement(GeoPlacemark place, double compassHeading)
        {
            if (left > compassHeading)
            {
                if (place.Orientation <= compassHeading + angularSpan && place.Orientation >= compassHeading)
                    return place.Orientation - compassHeading;
                else if (place.Orientation >= left || place.Orientation <= compassHeading)
                    return -(((compassHeading - place.Orientation) + 360) % 360);   //angle to the left
            }
            else if (right < compassHeading)
            {
                if (place.Orientation >= compassHeading - angularSpan && place.Orientation <= compassHeading)
                    return -(compassHeading - place.Orientation);                   //angle to the left
                else if (place.Orientation >= compassHeading || place.Orientation <= right)
                    return ((place.Orientation - compassHeading) + 360) % 360;
            }
            else
            {
                if (place.Orientation >= compassHeading - angularSpan && place.Orientation <= compassHeading)
                    return -(compassHeading - place.Orientation);                   //angle to the left
                else if (place.Orientation <= compassHeading + angularSpan && place.Orientation >= compassHeading)
                    return place.Orientation - compassHeading;
            }
            return -180;
        }
    }
}
