using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using AugmentedReality.Utilities;
using System.Threading;
using Microsoft.Devices;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using AugmentedReality.Models;
using System.Windows.Media.Imaging;

namespace AugmentedReality.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {                
        PhotoCamera camera;
        GeoCoordinateWatcher coordinateWatcher;
        Compass compass;
        bool watcherInitialized = false;
        bool calibrating = false;

        Pushpin userLocation;
        MapPolyline traceLine;


        GeoCoordinate oldPoint;
        double oldHeading = -10;
        Image arrow, CompassImage, directionImage;

        string sampleRoute = "72.85692904290141,19.11906242024381,0 72.85706160596182,19.11957858247711,0 72.85662091899002,19.1196203148946,0 72.85664720937551,19.1205128530729,0 72.85673305678101,19.12076850927489,0 72.85704989599842,19.12072052604721,0 72.85704670717149,19.12038809830598,0 72.85714849940776,19.12032323794295,0";
        AugmentedPlacemarks augmentedReality;

        //TestSamples
        //GeoCoordinate position = new GeoCoordinate { Latitude = 19.11906242024381, Longitude = 72.85692904290141 };
        //GeoCoordinate point1 = new GeoCoordinate { Latitude = 19.12032323794295, Longitude = 72.85714849940776 };

        public MainPage()
        {
            InitializeComponent();
            camera = new PhotoCamera(CameraType.Primary);
            viewFinderBrush.SetSource(camera);

            augmentedReality = new AugmentedPlacemarks();            

            camera = new PhotoCamera(CameraType.Primary);
            viewFinderBrush.SetSource(camera);
            userLocation = new Pushpin();
            userLocation.Background = new SolidColorBrush(Colors.Blue);
            oldPoint = new GeoCoordinate();

            traceLine = new MapPolyline()
            {
                Foreground = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2,
                Locations = new LocationCollection(),
                Opacity = 1.0
            };

            //Debug.WriteLine(LatLongMath.GetDistance(position, point1));
            

            StartupCode();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {            
            
        }

        private void StartupCode()
        {
            MapLayer.Children.Clear();
            MarkersLayer.Children.Clear();

            coordinateWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            coordinateWatcher.MovementThreshold = 1;
            coordinateWatcher.PositionChanged += coordinateWatcher_PositionChanged;
            coordinateWatcher.StatusChanged += coordinateWatcher_StatusChanged;
            coordinateWatcher.Start();
            watcherInitialized = false;
            InitializeImage(ref arrow, "/Assets/Arrow.png");
            InitializeImage(ref CompassImage, "/Assets/Compass.png");
            InitializeImage(ref directionImage, "/Assets/ArrowMetro.png");
        }        

        #region GPS

        void coordinateWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            watcherInitialized = false;
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:                    
                    break;

                case GeoPositionStatus.Initializing:
                    StatusBar.Text = "Initializing";
                    break;

                case GeoPositionStatus.NoData:
                    StatusBar.Text = "No data";
                    break;

                case GeoPositionStatus.Ready:
                    StatusBar.Text = "Data feed started";
                    watcherInitialized = true;
                    MapControl.ZoomLevel = 15;
                                        
                    UpdatePosition(coordinateWatcher.Position.Location);
                    traceLine.Locations.Add(userLocation.Location);
                    InitializeCompass();
                    break;
            }
        }

        void coordinateWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (watcherInitialized)
                UpdatePosition(e.Position.Location);
        }

        private void UpdatePosition(GeoCoordinate position)
        {            
            MapControl.Center = position;
            oldPoint = position;
                      
            augmentedReality.PositionUpdate(position);
            if (augmentedReality.Mode == AugmentedPlacemarks.AugmentedMode.Navigator)
            {    
                GeoPlacemark dummy;
                if ((dummy = augmentedReality.GetDummyNextPoint()) != null)
                {
                    MarkersLayer.Children.Clear();
                    MarkersLayer.Children.Add(new Pushpin { Background = new SolidColorBrush(Colors.Blue), Content = dummy.Name, Location = dummy.Position });

                    traceLine = new MapPolyline
                    {
                        Foreground = new SolidColorBrush(Colors.Red),
                        StrokeThickness = 2.0,
                        Locations = new LocationCollection()
                    };
                    foreach (GeoPlacemark place in augmentedReality.GetDummyPathWay())
                        traceLine.Locations.Add(place.Position);                   

                    traceLine.Locations.Insert(0, oldPoint);
                    MapLayer.Children.Clear();
                    MapLayer.Children.Add(traceLine);
                }
            }
        }

        #endregion

        #region Compass

        private void InitializeCompass()
        {
            try
            {
                compass = new Compass();
                if (Compass.IsSupported)
                {
                    compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(100);
                    compass.CurrentValueChanged += compass_CurrentValueChanged;
                    compass.Calibrate += compass_Calibrate;
                    compass.Start();
                }
                else
                    MessageBox.Show("Compass not supported on this device", "Message", MessageBoxButton.OK);
            }
            catch (Exception exp) { }
        }

        void compass_Calibrate(object sender, CalibrationEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                calibrating = true;
                AppGrid.Visibility = Visibility.Collapsed;
                CalibrateGrid.Visibility = Visibility.Visible;
            });
        }

        void compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (calibrating)
                {
                    accuracyTextBlock.Text = String.Format("{0:0}", e.SensorReading.HeadingAccuracy);
                    if (e.SensorReading.HeadingAccuracy <= 10)
                        accuracyTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    PositionAndRotateImage(oldPoint, AdjustHeading(e.SensorReading.TrueHeading));
                    //CompassReading.Text = AdjustHeading(e.SensorReading.TrueHeading).ToString();                       

                    if (augmentedReality.Mode == AugmentedPlacemarks.AugmentedMode.Explorer)
                        DisplayMarkers(AdjustHeading(e.SensorReading.TrueHeading));
                    else if (augmentedReality.Mode == AugmentedPlacemarks.AugmentedMode.Navigator)
                        DisplayNavigationMarker(AdjustHeading(e.SensorReading.TrueHeading));
                }
            });
        }

        private double AdjustHeading(double heading)
        {
            return (heading + 90) % 360;
        }

        private void OnTapDoneCalibrating(object sender, System.Windows.Input.GestureEventArgs e)
        {
            calibrating = false;
            AppGrid.Visibility = Visibility.Visible;
            CalibrateGrid.Visibility = Visibility.Collapsed;
        }

        #endregion

        private void InitializeImage(ref Image image, string path)
        {
            image = new Image();
            image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Relative));
            image.Opacity = 1.0;
            image.Stretch = Stretch.None;
            image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        }

        private void PositionAndRotateImage(GeoCoordinate position, double angle)
        {
            try
            {
                MapLayer.Children.Clear();
                AppGrid.Children.Remove(CompassImage);
                CompassImage.RenderTransform = new RotateTransform { Angle = angle };
                CompassImage.HorizontalAlignment = HorizontalAlignment.Right;
                CompassImage.VerticalAlignment = VerticalAlignment.Top;
                AppGrid.Children.Add(CompassImage);
                Grid.SetRow(CompassImage, 1);

                arrow.RenderTransform = new RotateTransform { Angle = angle };
                MapLayer.AddChild(arrow, position, PositionOrigin.Center);

                /*foreach (GeoCoordinate point in traceLine.Locations)
                    MapLayer.Children.Add(new Pushpin() { Location = point });*/
                //MapLayer.Children.Add(traceLine);
            }
            catch (Exception exp) { }
        }

        private void DisplayNavigationMarker(double angle)
        {
            augmentedReality.SetDisplaySize(ARGrid.ActualWidth, ARGrid.ActualHeight);
            ARGrid.Children.Clear();
            ARNavigateGrid.Children.Clear();            

            double imageWidth = 50, imageHeight = 65;
            GeoPlacemark placemark;

            try
            {
                placemark = augmentedReality.GetNextPoint(angle);
                if (placemark != null)
                {
                    directionImage.RenderTransform = new RotateTransform { Angle = ((placemark.Orientation - angle) + 360) % 360 };
                    directionImage.HorizontalAlignment = HorizontalAlignment.Right;
                    directionImage.VerticalAlignment = VerticalAlignment.Top;
                    //directionImage.Width = 90;
                    //directionImage.Stretch = Stretch.Uniform;
                    directionImage.Margin = new Thickness(10);
                    ARNavigateGrid.Children.Add(directionImage);

                    if (placemark.Angle != -180)
                    {
                        Image placemarkImage = new Image();
                        placemarkImage.Source = new BitmapImage(new Uri("/Assets/Placemark.png", UriKind.Relative));
                        placemarkImage.Width = imageWidth;
                        placemarkImage.Height = imageHeight;
                        placemarkImage.Stretch = Stretch.Fill;
                        placemarkImage.HorizontalAlignment = HorizontalAlignment.Left;
                        placemarkImage.VerticalAlignment = VerticalAlignment.Top;
                        placemarkImage.Margin = new Thickness(placemark.DisplayX - (placemarkImage.Width / 2), placemark.DisplayY - (placemarkImage.Height / 2), 0, 0);
                        ARNavigateGrid.Children.Add(placemarkImage);
                        NextDirectionTextBlock.Text = String.Format("Walk another {0:0} meters to the next point", placemark.Distance);                        
                    }
                }
                else
                {
                    NextDirectionTextBlock.Text = "You have reached the destination";
                }
            }
            catch (Exception exp) { }
        }

        private void DisplayMarkers(double angle)
        {
            augmentedReality.SetDisplaySize(ARGrid.ActualWidth, ARGrid.ActualHeight);
            List<GeoPlacemark> placemarksToShow = augmentedReality.VisiblePlacemarks(angle);            

            StackPanel basePanel;
            TextBlock placeText;
            double panelWidth = 170, panelHeight = 45;            
            
            ARGrid.Children.Clear();
            ARNavigateGrid.Children.Clear();
            MarkersLayer.Children.Clear();

            try
            {
                if (augmentedReality.Mode == AugmentedPlacemarks.AugmentedMode.Explorer)
                {
                    foreach (GeoPlacemark place in placemarksToShow)
                    {
                        placeText = new TextBlock();
                        placeText.Text = place.AdjustedName;
                        placeText.Foreground = new SolidColorBrush(Colors.White);
                        placeText.FontSize = 22;
                        //placeText.TextWrapping = TextWrapping.Wrap;                        
                        placeText.TextAlignment = TextAlignment.Left;                        
                        placeText.Margin = new Thickness(6, 0, 0, 0);

                        basePanel = new StackPanel
                        {
                            Background = new SolidColorBrush(Colors.Gray),
                            Opacity = 0.85,
                            Width = panelWidth,
                            Height = panelHeight
                        };

                        basePanel.Children.Add(placeText);
                        var gl = GestureService.GetGestureListener(basePanel);
                        gl.Tap += new EventHandler<GestureEventArgs>(TapOnPlacemark);                                            
                        basePanel.DataContext = place;

                        placeText = new TextBlock();
                        placeText.Margin = new Thickness(6, -6, 0, 0);
                        placeText.Foreground = new SolidColorBrush(Colors.LightGray);
                        placeText.Text = String.Format("{0:0} meters away", place.Distance);
                        placeText.FontSize = 15;
                        //placeText.TextWrapping = TextWrapping.Wrap;                        
                        placeText.TextAlignment = TextAlignment.Left;

                        basePanel.HorizontalAlignment = HorizontalAlignment.Left;
                        basePanel.VerticalAlignment = VerticalAlignment.Top;
                        basePanel.Children.Add(placeText);

                        basePanel.Margin = new Thickness(place.DisplayX - (panelWidth / 2), place.DisplayY - (panelHeight / 2), 0, 0);
                        ARGrid.Children.Add(basePanel);

                        MarkersLayer.Children.Add(new Pushpin { Background = new SolidColorBrush(Colors.Blue), Content = place.Name, Location = place.Position });
                    }
                }                
            }
            catch (Exception exp) { }
        }

        void TapOnPlacemark(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            GeoPlacemark place = (GeoPlacemark)(((StackPanel)sender).DataContext);
            DestinationTextBlock.Text = place.Name;
            NextDirectionTextBlock.Text = "calculating ...";
            NavigateParentGrid.Visibility = Visibility.Visible;
            augmentedReality.SwitchToNavigator(place);            
        }

        private void TapOnCancelNavigator(object sender, System.Windows.Input.GestureEventArgs e)
        {
            augmentedReality.SwitchToExplorer();
            NavigateParentGrid.Visibility = Visibility.Collapsed;
        }        
    }
}