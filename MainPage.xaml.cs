using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using BingMaps.BingMapGeoCodeService;
using Microsoft.Phone.Tasks;

namespace UberText
{
    public partial class MainPage : PhoneApplicationPage
    {
        private ProgressIndicator _progressIndicator = null;
        private GeoCoordinateWatcher _locationService = null;

        public MainPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            // initialize the location service
            _locationService = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            _locationService.MovementThreshold = 50;
            _locationService.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(locationService_PositionChanged);
            _locationService.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(locationService_StatusChanged);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_progressIndicator == null)
            {
                _progressIndicator = new ProgressIndicator();
                _progressIndicator.IsVisible = true;
                SystemTray.ProgressIndicator = _progressIndicator;
            }

            _locationService.Start();
        }

        void locationService_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled)
            {
                MessageBox.Show("Location services are not enabled for your phone. Uber works best when this feature is enabled.", "Location Disabled", MessageBoxButton.OK);
            }
            else if (e.Status == GeoPositionStatus.NoData)
            {
                MessageBox.Show("Your location could not be determined, please try again later. Uber will use your most recent location until a new location is determined.", "Location Unavailable", MessageBoxButton.OK);
            }
        }

        void locationService_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (_locationService.Status == GeoPositionStatus.Ready &&
                e.Position.Location.IsUnknown == false)
            {
                this.EnableProgressBar();
                this.txtAddress.Text = "Locating...";

                // clear map
                this.mapLocation.Children.Clear();

                // show current location
                Pushpin pin = new Pushpin();
                pin.Location = e.Position.Location;
                pin.Content = "My Location";
                pin.Background = (SolidColorBrush)Resources["PhoneAccentBrush"];

                // add location to map
                this.mapLocation.Children.Add(pin);

                this.mapLocation.SetView(e.Position.Location, 16.0);

                // reverse geocode the address
                ReverseGeocodeRequest request = new ReverseGeocodeRequest();

                request.Credentials = new Credentials();
                request.Credentials.ApplicationId = "AjeRbF5SN4mPdesrT0tBUM4BVw4L7gutyV9OjgxmJ7lHDOcyZL46aAJuRnhOC1rK";

                request.Location = new Microsoft.Phone.Controls.Maps.Platform.Location();
                request.Location.Latitude = e.Position.Location.Latitude;
                request.Location.Longitude = e.Position.Location.Longitude;

                GeocodeServiceClient service = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
                service.ReverseGeocodeCompleted += new EventHandler<ReverseGeocodeCompletedEventArgs>(service_ReverseGeocodeCompleted);
                service.ReverseGeocodeAsync(request);
            }
        }

        private void service_ReverseGeocodeCompleted(object sender, ReverseGeocodeCompletedEventArgs e)
        {
            // set address text
            if (e.Result.Results.Count > 0)
            {
                var result = e.Result.Results[0];
                this.txtAddress.Text = result.Address.FormattedAddress;
            }

            this.DisableProgressBar();
        }

        private void EnableProgressBar()
        {
            if (_progressIndicator != null)
                _progressIndicator.IsIndeterminate = true;
        }

        private void DisableProgressBar()
        {
            if (_progressIndicator != null)
                _progressIndicator.IsIndeterminate = false;
        }

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            SmsComposeTask task = new SmsComposeTask();
            task.To = "827222";
            task.Body = this.txtAddress.Text;

            task.Show();
        }

        private void mnuPrivacyPolicy_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This application uses your current location and shares it with Uber Technologies only when you Request a Pickup. Additional privacy policy information can be found online at http://www.uber.com/privacy.", "Privacy Policy", MessageBoxButton.OK);
        }

        private void mnuToggleLocation_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem item = ApplicationBar.MenuItems[1] as ApplicationBarMenuItem;

            if (item.Text == "turn off location")
            {
                MessageBox.Show("The location service has been stopped and this application is no longer detecting your location. Your location and address information will not be updated during this time.", "Disable Location", MessageBoxButton.OK);
                _locationService.Stop();

                item.Text = "turn on location";
            }
            else
            {
                MessageBox.Show("The location service has been restarted. It may take a few seconds for your location and address information to initialize.", "Enable Location", MessageBoxButton.OK);
                _locationService.Start();

                item.Text = "turn off location";
            }
        }

        private void mnuHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Using Uber with text messaging is simple:\n\n1. Text your pickup address and city to 827222.\n2. Uber will respond with a text message asking you to confirm your location.\n3. Uber will locate a car and text back with an estimated arrival time.\n4. Uber will send a text message when your driver has arrived.", "Uber Help", MessageBoxButton.OK);
        }
    }
}