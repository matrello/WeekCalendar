using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using SlickThought.Phone;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using weekc.Languages;
using weekcc;

namespace weekc
{
    public partial class MainPage : PhoneApplicationPage
    {
        const int WEEKGRID_HEADER_FONTSIZE_SMALL = 14;
        const int WEEKGRID_HEADER_FONTSIZE_LARGE = 18;
        const int WEEKGRID_SWITCHGRIDDOTS_MINHEIGHT = 68;   // should match WEEKGRID_CELL_TEXTSMALL_THRESHOLD

        PeriodicTask periodicTask;
        public bool agentIsEnabled = true;
        const string periodicTaskName = "WeekcAgent";

        private WeekGridController weekGridController;
        private WeekTileController weekTileController;
        private UIElement selectEventControl;
        private DateTime startWeekDay;

        private bool newPageInstance = false;
        private bool trialShow = false;
        private bool zoomIsEnabled = false;
        private bool navigationFromSubPage = false;
        private bool doDataBind = false;

        public MainPage()
        {
            InitializeComponent();
            newPageInstance = true;
            startWeekDay = DateTime.MinValue;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (weekGridController != null)
            {
                newPageInstance = false;
                State["StartWeekDay"] = weekGridController.StartWeekDay;
            }
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            navigationFromSubPage = false;
            if (PhoneApplicationService.Current.State.ContainsKey("fromSubPage"))
            {
                navigationFromSubPage = (bool)PhoneApplicationService.Current.State["fromSubPage"];
                PhoneApplicationService.Current.State["fromSubPage"] = (bool)false;
            }

            if (doDataBind)
            {
                doDataBind = false;
                weekGridController.DataBind();
            }

            trialShow = false;
            if (TrialManager.Current.IsTrial())
            {
                UsageExpirationPolicy trialManager = TrialManager.Current.ApplicationPolicy as UsageExpirationPolicy;

                if (trialManager.IsExpired || (!trialManager.IsExpired && !navigationFromSubPage))
                {
                    if (!trialManager.IsExpired)
                        trialManager.UsageCount++;

                    trialShow = true;
                }
            }

        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            SystemTray.IsVisible = false;

            if (weekGridController == null || AppSettingsChanged.Changed)
            {
                AppSettingsChanged.Changed = false;
                AppSettings appSettings = new AppSettings();
                zoomIsEnabled = appSettings.ZoomOnStartSetting;
                ApplicationBar.Mode = (zoomIsEnabled ? ApplicationBarMode.Minimized : ApplicationBarMode.Default);

                weekGridController = new WeekGridController(WeekGrid, WeekHeaderGrid, Event_ButtonClick);
                weekTileController = new WeekTileController(App.logger, Strings.Title, Strings.WeekShortLabel);

                TitleImage.Source = App.GetPageApplicationIcon();
            }

            if (State.ContainsKey("StartWeekDay") && newPageInstance)
                weekGridController.StartWeekDay = (DateTime)State["StartWeekDay"];

            #region init ApplicationBar

            if (ApplicationBar.Buttons.Count == 0)
            {
                ApplicationBarMenuItem settingsItem = new ApplicationBarMenuItem(Strings.SettingsItem);
                settingsItem.Click += new EventHandler(SettingsMenuItem_Click);

                ApplicationBarMenuItem aboutItem = new ApplicationBarMenuItem(Strings.AboutItem);
                aboutItem.Click += new EventHandler(AboutMenuItem_Click);

                string cultureFileName = "en-US";
                if (CultureInfo.CurrentCulture.Name == "it-IT" || CultureInfo.CurrentCulture.Name == "de-DE" || CultureInfo.CurrentCulture.Name == "fr-FR")
                    cultureFileName = CultureInfo.CurrentCulture.Name;

                ApplicationBarIconButton todayButton = new ApplicationBarIconButton(new Uri(
                    string.Format("/Icons/{0}.{1}.png", DateTime.Now.ToString("MMdd"), cultureFileName), UriKind.Relative));
                todayButton.Text = Strings.TodayButton;
                todayButton.Click += new EventHandler(TodayButton_Click);

                ApplicationBarMenuItem refreshItem = new ApplicationBarMenuItem(Strings.RefreshButton);
                refreshItem.Click += new EventHandler(RefreshButton_Click);

                ApplicationBarIconButton zoomButton = new ApplicationBarIconButton(new Uri("/Icons/zoom.png", UriKind.Relative));
                zoomButton.Text = Strings.ZoomButton;
                zoomButton.Click += new EventHandler(ZoomButton_Click);

                ApplicationBarIconButton searchButton = new ApplicationBarIconButton(new Uri("/Icons/appbar.feature.search.rest.png", UriKind.Relative));
                searchButton.Text = Strings.SearchTitle;
                searchButton.Click += new EventHandler(SearchButton_Click);

                ApplicationBar.MenuItems.Add(refreshItem);
                ApplicationBar.MenuItems.Add(settingsItem);
                ApplicationBar.MenuItems.Add(aboutItem);
                ApplicationBar.Buttons.Add(todayButton);
                ApplicationBar.Buttons.Add(zoomButton);
                ApplicationBar.Buttons.Add(searchButton);

#if WP8
                ApplicationBarIconButton newButton = new ApplicationBarIconButton(new Uri("/Icons/appbar.add.png", UriKind.Relative));
                newButton.Text = Strings.New;
                newButton.Click += new EventHandler(newButton_Click);
                ApplicationBar.Buttons.Add(newButton);
#endif
            }

            #endregion

            weekGridController.AdjustWidth();
            AdjustHeader();
            weekGridController.AdjustHeight(zoomIsEnabled);
            SwitchGridDots();

            if (!navigationFromSubPage)
                weekGridController.DataBind();
            
            PopulateHeader();
            PopulateTitle();

            weekTileController.TileRefresh(false);

            ImageBrush ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri(App.IsDark ? "/Icons/appbar.marketplace.dark.png" : "/Icons/appbar.marketplace.light.png", UriKind.Relative));
            ib.AlignmentX = AlignmentX.Left;
            ib.Stretch = Stretch.Uniform;
            StoreButton.Background = ib;

            if (trialShow)
            {
                VersionText.DataContext = null;
                VersionText.DataContext = App.GetAppVersion();
                TrialStroke.Stroke = ColorExtensions.GetBlendedBrush((SolidColorBrush)App.Current.Resources["PhoneAccentBrush"], 0.9);
                TrialFill.Fill = ColorExtensions.GetBlendedBrush((SolidColorBrush)App.Current.Resources["PhoneAccentBrush"], 0.3);
                TrialGrid.Visibility = System.Windows.Visibility.Visible;

                if (TrialManager.Current.IsExpired)
                {
                    weekGridController.ExpiredMode = true;
                    TrialText.Text = Strings.TrialExpired;
                }
                else
                    TrialText.Text = Strings.TrialText;

                //UsageExpirationPolicy trialManager = TrialManager.Current.ApplicationPolicy as UsageExpirationPolicy;
                //VersionText.Text = trialManager.UsageCount.ToString() + " " + trialManager.MaxUsage.ToString();
            }

            StartPeriodicAgent();
        }

        void WeekSlideLeft1_Completed(object sender, EventArgs e)
        {
            WeekSlideLeft1.Stop();
            weekGridController.WeekNext();
            PopulateHeader();
            weekGridController.DataBind();

            while (TitleFadeOut.GetCurrentState() != ClockState.Stopped)
                System.Threading.Thread.Sleep(10);

            PopulateTitle();
            TitleFadeIn.Begin();

            App.logger.AppendInfo("WeekSlideLeft1_Completed calling WeekSlideLeft2.Begin()");
            WeekSlideLeft2.Begin();
        }

        void WeekSlideRight1_Completed(object sender, EventArgs e)
        {
            WeekSlideRight1.Stop();
            weekGridController.WeekPrevious();
            PopulateHeader();
            weekGridController.DataBind();

            while (TitleFadeOut.GetCurrentState() != ClockState.Stopped)
                System.Threading.Thread.Sleep(5);

            PopulateTitle();
            TitleFadeIn.Begin();

            App.logger.AppendInfo("WeekSlideRight1_Completed calling WeekSlideRight2.Begin()");
            WeekSlideRight2.Begin();
        }

        private void GestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            if (e.Direction == System.Windows.Controls.Orientation.Horizontal)
            {
                if (!weekGridController.IsUpdated)
                {
                    App.logger.AppendInfo("GestureListener_Flick gesture aborted: weekGridController.IsUpdated = false");
                    return;
                }

                App.logger.AppendInfo("HorizontalVelocity: {0}", e.HorizontalVelocity.ToString());

                if (e.HorizontalVelocity < 0)
                    WeekSlideLeft1.Begin();
                else
                    WeekSlideRight1.Begin();

                TitleFadeOut.Begin();
            }
        }

        public void PopulateTitle()
        {
            int weekNumber = 0;
            AppSettings appSettings = new AppSettings();

            if (appSettings.ShowWeekNumberSetting)
            {
                var systemCalendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar; 
                weekNumber = systemCalendar.GetWeekOfYear( 
                    weekGridController.StartWeekDay, 
                    System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.CalendarWeekRule, 
                    System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            }

            if (weekGridController.StartWeekDay.Month != weekGridController.StartWeekDay.AddDays(6).Month)
            {
                ApplicationTitle.Text = string.Format("{0} {1}{2} - {3}",
                    Strings.Title,
                    (appSettings.ShowWeekNumberSetting ? weekNumber + ": " : ""),
                    weekGridController.StartWeekDay.ToString("d MMM"),
                    weekGridController.StartWeekDay.AddDays(6).ToString("d MMM yyyy"));
            }
            else
            {
                ApplicationTitle.Text = string.Format("{0} {1}{2}-{3} {4}",
                    Strings.Title,
                    (appSettings.ShowWeekNumberSetting ? weekNumber + ": " : ""),
                    weekGridController.StartWeekDay.Day,
                    weekGridController.StartWeekDay.AddDays(6).Day,
                    weekGridController.StartWeekDay.ToString("MMMM yyyy"));
            }
        }

        public void AdjustHeader()
        {

            AppSettings appSettings = new AppSettings();
            int headerFontSize = (appSettings.ShowWeekendSetting ? WEEKGRID_HEADER_FONTSIZE_SMALL : WEEKGRID_HEADER_FONTSIZE_LARGE);

            HeaderDay1.FontSize = headerFontSize;
            HeaderDay2.FontSize = headerFontSize;
            HeaderDay3.FontSize = headerFontSize;
            HeaderDay4.FontSize = headerFontSize;
            HeaderDay5.FontSize = headerFontSize;
            HeaderDay6.FontSize = headerFontSize;
            HeaderDay7.FontSize = headerFontSize;
        }

        public void PopulateHeader()
        {
            TodayAccentBorder.Stop();

            SetHeaderText(HeaderDay1, weekGridController.StartWeekDay);
            SetHeaderText(HeaderDay2, weekGridController.StartWeekDay.AddDays(1));
            SetHeaderText(HeaderDay3, weekGridController.StartWeekDay.AddDays(2));
            SetHeaderText(HeaderDay4, weekGridController.StartWeekDay.AddDays(3));
            SetHeaderText(HeaderDay5, weekGridController.StartWeekDay.AddDays(4));
            SetHeaderText(HeaderDay6, weekGridController.StartWeekDay.AddDays(5));
            SetHeaderText(HeaderDay7, weekGridController.StartWeekDay.AddDays(6));
        }

        private void SetHeaderText(TextBlock header, DateTime day)
        {
            header.Text = day.ToString("ddd d");
            if (day == App.DateTimeTrim(DateTime.Now, TimeSpan.TicksPerDay))
            {
                Storyboard.SetTarget(TodayAccentBorder, (Border)header.Parent);
                TodayAccentBorder.Begin();
            }
        }

        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void TodayButton_Click(object sender, EventArgs e)
        {
            weekGridController.WeekToday();
            AdjustHeader();
            PopulateHeader();
            PopulateTitle();
            weekGridController.DataBind();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {

            weekGridController.DataBind();

        }

        private void ZoomButton_Click(object sender, EventArgs e)
        {
            if (weekGridController != null)
            {
                zoomIsEnabled = !zoomIsEnabled;
                ApplicationBar.Mode = (zoomIsEnabled ? ApplicationBarMode.Minimized : ApplicationBarMode.Default);
                weekGridController.AdjustHeight(zoomIsEnabled);
                SwitchGridDots();
                weekGridController.DataBind();
            }
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/Search.xaml", UriKind.Relative));
        }

#if WP8
        void newButton_Click(object sender, EventArgs e)
        {
            // set startTime to the same DayOfWeek of today, but of the current shown week.
            // the new event begins at current Hour, rounded to :00
            DateTime startTime = weekGridController.StartWeekDay;
            while (startTime.DayOfWeek != DateTime.Now.DayOfWeek)
                startTime = startTime.AddDays(1);
            startTime = startTime.AddHours(DateTime.Now.Hour);

            try
            {
                SaveAppointmentTask saveAppointmentTask = new SaveAppointmentTask();
                saveAppointmentTask.StartTime = startTime;
                saveAppointmentTask.Show();
                doDataBind = true;
            }
            catch { }
        }
#endif

        private void WeekHeaderGrid_LayoutUpdated(object sender, EventArgs e)
        {
            // this makes the header with weekdays to align with the lower main grid
            WeekHeaderGrid.ColumnDefinitions[0].Width = new GridLength(WeekGrid.ColumnDefinitions[0].ActualWidth);
        }

        private void TitleFadeOut_Completed(object sender, EventArgs e)
        {
            TitleFadeOut.Stop();
        }

        void Event_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (EventSelectedIn.GetCurrentState() == ClockState.Stopped)
            {
                selectEventControl = (UIElement)sender;

                Storyboard.SetTarget(EventSelectedIn, selectEventControl);
                EventSelectedIn.Begin();
            }
        }

        private void EventSelectedIn_Completed(object sender, EventArgs e)
        {
            if (EventSelectedIn.GetCurrentState() != ClockState.Stopped)
            {
                EventSelectedIn.Stop();
                EventSelectedIn.Seek(new TimeSpan(0));
            }

            if (EventSelectedOut.GetCurrentState() != ClockState.Stopped)
            {
                EventSelectedOut.SkipToFill();
                EventSelectedOut.Stop();
            }

            Storyboard.SetTarget(EventSelectedOut, selectEventControl);

            App.SelectedAppointment = ((Button)selectEventControl).Tag as Appointment;
            EventSelectedOut.Begin();
            selectEventControl = null;
        }

        private void EventSelectedOut_Completed(object sender, EventArgs e)
        {
            if (WeekSlideLeft1.GetCurrentState() != ClockState.Stopped ||
                WeekSlideLeft2.GetCurrentState() != ClockState.Stopped ||
                WeekSlideRight1.GetCurrentState() != ClockState.Stopped ||
                WeekSlideRight2.GetCurrentState() != ClockState.Stopped)
                App.logger.AppendInfo("EventSelectedOut_Completed: click event skipped due to weekslide still in progress.");
            else
                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));        
        }

        private void WeekSlideLeft2_Completed(object sender, EventArgs e)
        {
            WeekSlideLeft2.Stop();
        }

        private void WeekSlideRight2_Completed(object sender, EventArgs e)
        {
            WeekSlideRight2.Stop();
        }

        private void StartPeriodicAgent()
        {
            // http://msdn.microsoft.com/en-us/library/hh202941(v=VS.92).aspx

            agentIsEnabled = true;

            periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;
            if (periodicTask != null)
                RemoveAgent(periodicTaskName);

            periodicTask = new PeriodicTask(periodicTaskName);
            periodicTask.Description = Strings.TaskLabel;

            try
            {
                ScheduledActionService.Add(periodicTask);

#if(DEBUG_AGENT)
                ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(30));
#endif

            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    App.logger.AppendInfo("Background agents for this application have been disabled by the user.");
                    agentIsEnabled = false;
                }
            }
        }

        private void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (weekGridController != null)
            {
                bool isHeigthChanged = weekGridController.AdjustHeight(zoomIsEnabled);
                SwitchGridDots();

                if (zoomIsEnabled && isHeigthChanged)
                    weekGridController.DataBind();
            }
        }

        private void AdjustYPosition()
        {
            // scrolls to the Y position reflecting current hour
            AppSettings appSettings = new AppSettings();
            double offset = WeekGrid.RowDefinitions[0].Height.Value + 
                (WeekGrid.RowDefinitions[1].Height.Value * (DateTime.Now.Hour - 1 - appSettings.DayBeginsSetting));

            WeekScroll.ScrollToVerticalOffset(offset);
        }

        private void SwitchGridDots()
        {
            bool on = (WeekGrid.RowDefinitions[2].Height.Value < WEEKGRID_SWITCHGRIDDOTS_MINHEIGHT ? false : true);

            #region uncomment to show row heigth to the title bar (debug)
            //ApplicationTitle.Text = WeekGrid.RowDefinitions[2].Height.Value.ToString();
            #endregion

            GridDot1.StrokeThickness = (on ? 1: 0);
            GridDot2.StrokeThickness = (on ? 1 : 0);
            GridDot3.StrokeThickness = (on ? 1 : 0);
            GridDot4.StrokeThickness = (on ? 1 : 0);
            GridDot5.StrokeThickness = (on ? 1 : 0);
            GridDot6.StrokeThickness = (on ? 1 : 0);
            GridDot7.StrokeThickness = (on ? 1 : 0);
            GridDot8.StrokeThickness = (on ? 1 : 0);
            GridDot9.StrokeThickness = (on ? 1 : 0);
            GridDot10.StrokeThickness = (on ? 1 : 0);
            GridDot11.StrokeThickness = (on ? 1 : 0);
            GridDot12.StrokeThickness = (on ? 1 : 0);
            GridDot13.StrokeThickness = (on ? 1 : 0);
            GridDot14.StrokeThickness = (on ? 1 : 0);
            GridDot15.StrokeThickness = (on ? 1 : 0);
            GridDot16.StrokeThickness = (on ? 1 : 0);
            GridDot17.StrokeThickness = (on ? 1 : 0);
            GridDot18.StrokeThickness = (on ? 1 : 0);
            GridDot19.StrokeThickness = (on ? 1 : 0);
            GridDot20.StrokeThickness = (on ? 1 : 0);
            GridDot21.StrokeThickness = (on ? 1 : 0);
            GridDot22.StrokeThickness = (on ? 1 : 0);
            GridDot23.StrokeThickness = (on ? 1 : 0);
            GridDot24.StrokeThickness = (on ? 1 : 0);
        }

        private void WeekGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustYPosition();
        }

        private void GestureListener_Hold(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {

#if WP8
            int holdRow, holdCol;
            double beamer = 0;
            bool add30mins = false;
            Point point = e.GetPosition(WeekGrid);

            for (holdRow = 0; holdRow < WeekGrid.RowDefinitions.Count; holdRow++ )
            {
                beamer += WeekGrid.RowDefinitions[holdRow].ActualHeight;
                if (beamer > point.Y)
                {
                    add30mins = (point.Y >= (beamer - (WeekGrid.RowDefinitions[holdRow].ActualHeight / 2)));
                    break;
                }
            }

            beamer = 0;
            for (holdCol = 0; holdCol < WeekGrid.ColumnDefinitions.Count; holdCol++)
            {
                beamer += WeekGrid.ColumnDefinitions[holdCol].ActualWidth;
                if (beamer > point.X)
                    break;
            }

            DateTime startTime = weekGridController.StartWeekDay.AddDays(holdCol - 1);
            startTime = startTime.AddHours((new AppSettings()).DayBeginsSetting + holdRow - 1);
            startTime = startTime.AddMinutes(add30mins ?  30 : 0);

            try
            {
                SaveAppointmentTask saveAppointmentTask = new SaveAppointmentTask();
                saveAppointmentTask.StartTime = startTime;
                saveAppointmentTask.IsAllDayEvent = (holdRow == 0);
                saveAppointmentTask.Show();
                doDataBind = true;
            }
            catch { }
#endif

        }

        private void Trial_Click(object sender, RoutedEventArgs e)
        {

            TrialGrid.Visibility = System.Windows.Visibility.Collapsed;
        
        }

        private void Store_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "WeekCalendar@live.com";
            emailComposeTask.Body = "Week Calendar version " + App.GetAppVersion() +"\n\n";
            emailComposeTask.Subject = "Week Calendar version " + App.GetAppVersion();
            emailComposeTask.Show();
        }
    }
}