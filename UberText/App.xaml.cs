using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using UberText.Common;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace UberText
{
    public partial class App : Application
    {
        public static string FeedbackEmailAddress = "feedback@mbmccormick.com";

        public static string VersionNumber
        {
            get
            {
                string assembly = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                string[] version = assembly.Split('=')[1].Split(',')[0].Split('.');

                return version[0] + "." + version[1];
            }
        }

        public static string ExtendedVersionNumber
        {
            get
            {
                string assembly = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                string[] version = assembly.Split('=')[1].Split(',')[0].Split('.');

                return version[0] + "." + version[1] + "." + version[2];
            }
        }

        public static string PlatformVersionNumber
        {
            get
            {
                return System.Environment.OSVersion.Version.ToString(3);
            }
        }

        public static PhoneApplicationFrame RootFrame { get; private set; }

        public App()
        {
            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
        }

        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            SmartDispatcher.Initialize(RootFrame.Dispatcher);
        }

        public static void PromptForMarketplaceReview()
        {
            string currentVersion = App.VersionNumber;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>("CurrentVersion", out currentVersion) == false)
                currentVersion = App.VersionNumber;

            DateTime installDate = DateTime.UtcNow;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<DateTime>("InstallDate", out installDate) == false)
                installDate = DateTime.UtcNow;

            if (currentVersion != App.VersionNumber) // override if this is a new version
                installDate = DateTime.UtcNow;

            if (DateTime.UtcNow.AddDays(-3) >= installDate) // prompt after 3 days
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = "Review Uber Text",
                    Message = "It's been a few days since you downloaded Uber Text. Would you like to leave a review for it on the Windows Phone Store?",
                    LeftButtonContent = "yes",
                    RightButtonContent = "no",
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                            marketplaceReviewTask.Show();

                            installDate = DateTime.MaxValue; // they have rated, don't prompt again

                            break;
                        default:
                            installDate = DateTime.UtcNow; // they did not rate, prompt again in 2 days
                            break;
                    }
                };

                messageBox.Show();
            }

            IsolatedStorageSettings.ApplicationSettings["CurrentVersion"] = currentVersion; // save current version of application
            IsolatedStorageSettings.ApplicationSettings["InstallDate"] = installDate; // save install date
        }

        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            LittleWatson.ReportException(e.ExceptionObject, null);

            RootFrame.Dispatcher.BeginInvoke(() =>
            {
                LittleWatson.CheckForPreviousException(false);
            });

            e.Handled = true;

            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

            GlobalLoading.Instance.Initialize(RootFrame);

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // If the app has received a 'reset' navigation, then we need to check
            // on the next navigation to see if the page stack should be reset
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Unregister the event so it doesn't get called again
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Only clear the stack for 'new' (forward) and 'refresh' navigations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // For UI consistency, clear the entire page stack
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // do nothing
            }
        }

        #endregion
    }
}