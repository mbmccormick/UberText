using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Device.Location;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using UberText.Common;
using Microsoft.Phone.Tasks;
using UberText.Geocoding;

namespace UberText
{
    public partial class MainPage : PhoneApplicationPage
    {
        private GeoCoordinateWatcher locationService;

        public MainPage()
        {
            InitializeComponent();

            locationService = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            locationService.MovementThreshold = 25;
            locationService.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<System.Device.Location.GeoCoordinate>>(locationService_PositionChanged);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoading = true;

            if (e.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                App.PromptForMarketplaceReview();
            }

            locationService.Start();
        }

        private void locationService_PositionChanged(object sender, GeoPositionChangedEventArgs<System.Device.Location.GeoCoordinate> e)
        {
            this.txtLocation.Text = "Loading...";

            if (locationService.Status == GeoPositionStatus.Ready &&
                e.Position.Location.IsUnknown == false)
            {
                GlobalLoading.Instance.IsLoading = true;

                this.mapLocation.Center = e.Position.Location;

                PlaceCurrentLocationPushpin(e.Position.Location.Latitude, e.Position.Location.Longitude);

                GeocodeClient client = new GeocodeClient();
                client.GeocodeAddress(e.Position.Location.Latitude + ", " + e.Position.Location.Longitude, false, (result) =>
                {
                    SmartDispatcher.BeginInvoke(() =>
                    {
                        foreach (var item in result.Results)
                        {
                            if (item.Types.Contains("street_address") == true ||
                                item.Types.Contains("route") == true)
                            {
                                string formattedAddress = item.FormattedAddress.Replace(", USA", "");
                                string line1 = formattedAddress.Split(',')[0].Trim();
                                string line2 = formattedAddress.Replace(line1, "").Substring(2);

                                this.txtLocation.Text = line1 + "\n" + line2;
                            }
                        }

                        GlobalLoading.Instance.IsLoading = false;
                    });
                });

            }
        }

        private void PlaceCurrentLocationPushpin(double latitude, double longitude)
        {
            this.mapLocation.Layers.Clear();

            MapLayer layer = new MapLayer();
            this.mapLocation.Layers.Add(layer);

            TextBlock tb = new TextBlock();
            tb.TextAlignment = TextAlignment.Center;
            tb.Text = "Current Location";

            Pushpin pp = new Pushpin();
            pp.Content = tb;
            pp.Margin = new Thickness(0, -60, 0, 0);

            MapOverlay mo = new MapOverlay();
            mo.Content = pp;
            mo.GeoCoordinate = new System.Device.Location.GeoCoordinate(latitude, longitude);

            layer.Add(mo);
        }

        private void mnuSend_Click(object sender, EventArgs e)
        {
            if (this.txtLocation.Text == "Loading...") return;

            SmsComposeTask task = new SmsComposeTask();
            task.To = "827222";
            task.Body = this.txtLocation.Text;

            task.Show();
        }

        private void mnuHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Using Uber with text messaging is simple:\n\n1. Text your pickup address and city to 827222.\n2. Uber will respond with a text message asking you to confirm your location.\n3. Uber will locate a car and text back with an estimated arrival time.\n4. Uber will send a text message when your driver has arrived.", "Uber Help", MessageBoxButton.OK);
        }

        private void mnuToggleLocation_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem target = (ApplicationBarMenuItem)sender;

            if (target.Text == "turn location off")
            {
                target.Text = "turn location on";
                locationService.Stop();
            }
            else
            {
                target.Text = "turn location off";
                locationService.Start();
            }
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuFeedback_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "feedback@mbmccormick.com";
            emailComposeTask.Subject = "Uber Text Feedback";
            emailComposeTask.Body = "Version " + App.ExtendedVersionNumber + " (" + App.PlatformVersionNumber + ")\n\n";
            emailComposeTask.Show();
        }
    }
}