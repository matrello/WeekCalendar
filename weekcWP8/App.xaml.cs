using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using weekc.Languages;
using weekcc;


namespace weekc
{
    public partial class App : Application
    {
        public static bool beta;
        public static Random rnd;
        public static Logger logger;
        public static Appointment SelectedAppointment;

        public static bool IsDark
        {
            get
            {
                return ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible);
            }
        }

        public static ImageSource GetPageApplicationIcon()
        {
            return new BitmapImage(new Uri(IsDark ? "/Icons/ApplicationIconSmallWhite.png" : "/Icons/ApplicationIconSmallBlack.png", UriKind.Relative));
        }

        public static DateTime DateTimeTrim(DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks);
        }

        public static string GetAppVersion()
        {
            string version;

            var asm = Assembly.GetExecutingAssembly();
            var parts = asm.FullName.Split(',');
            version = parts[1].Split('=')[1];

            if (App.beta)
                version += " BETA";

            return version;
        }

        public TransitionFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            rnd = new Random();
            logger = new Logger("Week Calendar");

            // if true shows "BETA" label in Version string
            beta = false;

            // Global handler for uncaught exceptions.
            UnhandledException += Application_UnhandledException;

            // Standard XAML initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
            if (Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode,
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Prevent the screen from turning off while under the debugger by disabling
                // the application's idle detection.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
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
            // http://mobileworld.appamundi.com/blogs/andywigley/archive/2010/11/24/best-of-breed-page-rotation-animations.aspx
            // http://blogs.msdn.com/b/delay/archive/2010/09/28/this-one-s-for-you-gregor-mendel-code-to-animate-and-fade-windows-phone-orientation-changes-now-supports-a-new-mode-hybrid.aspx

            RootFrame = new Delay.HybridOrientationChangesFrame();
            ((Delay.HybridOrientationChangesFrame)RootFrame).Duration = TimeSpan.FromSeconds(0.6);
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Handle reset requests for clearing the backstack
            RootFrame.Navigated += CheckForResetNavigation;

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

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(Strings.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), Strings.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }

#if FAKE_APPOINTMENT

    // this is used to simulate the presence of Appointments on WP7 emulator
    public class Appointment
    {
        public Account Account;
        public DateTime StartTime;
        public DateTime EndTime;
        public string Subject { get; set; }
        public string Details { get; set; }
        public bool IsAllDayEvent;
        public bool IsPrivate { get; set; }
        public string Location { get; set; }
        public AppointmentStatus Status;
        public Attendee Organizer { get; set; }
        public IEnumerable<Attendee> Attendees
        {
            get
            {
                return attendees;
            }
        }

        List<Attendee> attendees;

        public Appointment()
        {
            attendees = new List<Attendee>();
        }

        public void AddAttendee(Attendee att)
        {
            attendees.Add(att);
        }
    }

    public class Attendee
    {
        public string DisplayName { get; set; }
        public string EmailAddress { get; set; }
    }

#endif

    /// <summary>
    /// This class extends the ListPicker featuring Set for SelectedItems
    /// http://mobileworld.appamundi.com/blogs/andywigley/archive/2012/02/02/how-to-databind-selecteditems-of-the-listpicker-and-recurringdayspicker.aspx
    /// </summary>
    public class ListPickerEx : ListPicker
    {
        /// <summary>
        /// Gets or sets the selected items.
        /// </summary>
        public new IList SelectedItems
        {
            get
            {
                return (IList)GetValue(SelectedItemsProperty);
            }
            set
            {
                base.SetValue(SelectedItemsProperty, value);
            }
        }
    }

    public class AppointmentDrawHelper
    {
        public bool IsPrivate { get; set; }
        public Thickness BorderMargin { get; set; }
        public Brush BorderBrush { get; set; }
        public Brush Background { get; set; }
        public double StrokeThickness { get; set; }
        public Style TextStyle { get; set; }
        public Visibility ArrowVisible { get; set; }
        public string Location { get; set; }

        private string subject;

        public string Subject
        {
            get
            {
                if (IsPrivate)
                    return Strings.PrivateLabel;
                else
                    return subject;
            }
            set
            {
                subject = value;
            }
        }
    }

}