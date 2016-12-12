using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using weekc.Languages;
using weekcc;
using Microsoft.Phone.UserData;
using System.Windows.Data;

namespace weekc
{
    // FM.2016.12.12 - released to the public. https://github.com/matrello/WeekCalendar

    public struct WeekGridEvent
    {
        public Button GridEvent;
        public int Overlapping;
        public DateTime StartTime;
        public DateTime EndTime;
        public bool IsAllDayEvent;
    }

    public class WeekGridController
    {
        const double WEEKGRID_CELL_BACKGROUND_OPACITY = 0.20;
        const int    WEEKGRID_CELL_MINHEIGHT = 120;
        const int    WEEKGRID_EVENT_MINDURATION = 15;
        const int    WEEKGRID_EVENT_OVERLAP_INDENTATION = 30;
        const int    WEEKGRID_ALLDAYROW_OFFSET = 1;
        const int    WEEKGRID_ALLDAYROW_MINHEIGHT = 66;
        const int    WEEKGRID_CELL_TEXTSMALL_THRESHOLD = 68;    // should match WEEKGRID_SWITCHGRIDDOTS_MINHEIGHT

        private int dayBeginsSetting;
        private int dayEndsSetting;
        private bool showWeekendSetting;
        private bool showPrivateSetting;
        private List<AccountSetting> accountsSetting;
     
        private bool isUpdated = false;
        private Grid weekGrid, weekHeaderGrid;
        private DateTime startWeekDay = DateTime.MinValue;
        private RoutedEventHandler eventButtonClick;

        private List<Appointment> weekAppts = new List<Appointment>();  // returned from WP7 or simulated on WP Emulator
        private List<WeekGridEvent>[] weekGridEvents;                   // used to determine overlapped events

        public bool ExpiredMode = false;

        public WeekGridController(Grid WeekGrid, Grid WeekHeaderGrid, RoutedEventHandler EventButtonClick)
        {
            accountsSetting = new List<AccountSetting>();
            AppSettings appSettings = new AppSettings();
            dayBeginsSetting = appSettings.DayBeginsSetting;
            dayEndsSetting = appSettings.DayEndsSetting;
            showWeekendSetting = appSettings.ShowWeekendSetting;
            showPrivateSetting = appSettings.ShowPrivateSetting;
            foreach (AccountSetting account in appSettings.AccountsSetting)
                accountsSetting.Add(account);

            weekGrid = WeekGrid;
            weekHeaderGrid = WeekHeaderGrid;
            eventButtonClick = EventButtonClick;
            WeekToday();
        }

        public bool IsUpdated
        {
            get { return this.isUpdated; }
        }

        public DateTime StartWeekDay
        {
            get { return this.startWeekDay; }
            set
            {
                this.startWeekDay = value;
                isUpdated = false;
            }
        }

        public void WeekToday()
        {
            int delta = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek - DateTime.Now.DayOfWeek;
            startWeekDay = App.DateTimeTrim(DateTime.Now.AddDays(delta), TimeSpan.TicksPerDay);
            isUpdated = false;
        }

        public void WeekNext()
        {
            startWeekDay = startWeekDay.Add(new TimeSpan(7, 0, 0, 0));
            isUpdated = false;
        }

        public void WeekPrevious()
        {
            startWeekDay = startWeekDay.Add(new TimeSpan(-7, 0, 0, 0));
            isUpdated = false;
        }

        public void DataBind()
        {
            isUpdated = false;

            weekAppts.Clear();
            weekGridEvents = new List<WeekGridEvent>[8];
            for(int k=0; k<8; k++)
                weekGridEvents[k] = new List<WeekGridEvent>();

            Microsoft.Phone.UserData.Appointments appts = new Microsoft.Phone.UserData.Appointments();
            appts.SearchCompleted += new EventHandler<Microsoft.Phone.UserData.AppointmentsSearchEventArgs>(appts_SearchCompleted);

            App.logger.AppendInfo("WGC.DataBind(): begin appts searching from {0}", startWeekDay.ToString());
            appts.SearchAsync(startWeekDay, startWeekDay.AddDays(7), int.MaxValue, "WeekGrid");

            Clear();
            PopulateFirstColumn();
            App.logger.AppendInfo("WGC.DataBind(): WeekGrid cleared");
        }

        public void appts_SearchCompleted(object sender, Microsoft.Phone.UserData.AppointmentsSearchEventArgs e)
        {
            App.logger.AppendInfo("WGC.appts_SearchCompleted(): from {0} to {1}", e.StartTimeInclusive.ToString(), e.EndTimeInclusive.ToString());

#if FAKE_APPOINTMENT

            // this is used to simulate the presence of Appointments on WP7 emulator
            for(int k = 0; k<6; k++)
            {
                string[] locations = { "Rome", "Zug", "Zurich", "Cham" };
                string[] subjects = { "Geek time", "Play Assassin Creed III", "WC demo meeting", "Job meeting", "Meet Alessia", "Meet Ilaria" };
                DateTime startd = startWeekDay.AddDays(App.rnd.Next(0, 7));
                DateTime startd1 = startd;

                int starth = App.rnd.Next(0, 5) + 9;
                int endh = starth + App.rnd.Next(0, 4);
                int startm = 30 * App.rnd.Next(0, 2);
                int endm = 30 * App.rnd.Next(0, 2);
                int locationn = App.rnd.Next(0, 4);
                int status = App.rnd.Next(0, 9);
                int subjectn = App.rnd.Next(0, 5);
                int meeting = App.rnd.Next(0, 2);
                int allday = (k < 3 ? 1 : 0);

                Appointment cappt = new Appointment();
                if (allday == 1)
                {
                    cappt.IsAllDayEvent = true;
                    startd1 = startd.AddDays(App.rnd.Next(0, 3) + 1);
                    starth = startm = endh = endm = 0;
                }

                //startd1 = startd.AddDays(1);
                //starth = 15;
                //endh = 16;

                cappt.StartTime = new DateTime(startd.Year, startd.Month, startd.Day, starth, startm, 00);
                cappt.EndTime = new DateTime(startd1.Year, startd1.Month, startd1.Day, endh, endm, 00);
                cappt.Subject = subjects[subjectn];
                cappt.Details = "Dictumst eleifend facilisi faucibus, dictumst eleifend facilisi faucibus.";
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Location = locations[locationn]; // cappt.StartTime.ToShortDateString() + " " + cappt.EndTime.ToShortDateString();
                cappt.Status = (status == 2 ? Microsoft.Phone.UserData.AppointmentStatus.Tentative : Microsoft.Phone.UserData.AppointmentStatus.Busy);

                if (meeting == 1)
                {
                    Attendee atn = new Attendee();
                    atn.DisplayName = "Annamaria";
                    atn.EmailAddress = "anna.ascione@gmail.com";
                    cappt.AddAttendee(atn);
                    atn = new Attendee();
                    atn.DisplayName = "Mike1";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.AddAttendee(atn);
                    atn = new Attendee();
                    atn.DisplayName = "Mike2";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.AddAttendee(atn);
                    atn = new Attendee();
                    atn.DisplayName = "Mike3";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.AddAttendee(atn);
                    atn = new Attendee();
                    atn.DisplayName = "Mike4";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.AddAttendee(atn);
                    atn = new Attendee();
                    atn.DisplayName = "Mike5";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.AddAttendee(atn);

                    atn = new Attendee();
                    atn.DisplayName = "Mike6";
                    atn.EmailAddress = "michele.ceci@tin.it";
                    cappt.Organizer = atn;
                }

                weekAppts.Add(cappt);
            }

#else
            foreach (Appointment appt in e.Results)
                weekAppts.Add(appt);
#endif

            App.logger.AppendInfo("WGC.appts_SearchCompleted(): retrieved {0} appts", weekAppts.Count);

            weekAppts = weekAppts.OrderBy(p => p.StartTime).ThenBy(p => p.EndTime).ToList();
            App.logger.AppendInfo("WGC.appts_SearchCompleted(): sorted appts");

            foreach (Appointment appt in weekAppts)
            {
                if (IsAccountEnabled(appt))
                {
                    var dummyEvent = appt;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        AddEvent(dummyEvent);
                    });
                }
            }

            isUpdated = true;
        }

        public bool AddEvent(Appointment newEvent)
        {
            DateTime startTime = newEvent.StartTime;
            DateTime endTime = newEvent.EndTime;
            bool continued = false;

            if (ExpiredMode)
            {
                App.logger.AppendInfo("WGC.AddEvent(): skipped: ExpiredMode true: event won't be added.");
                return false;
            }

            bool isAllDayEvent = newEvent.IsAllDayEvent;

            if (newEvent.StartTime.Day != newEvent.EndTime.Day || newEvent.StartTime.Month != newEvent.EndTime.Month || newEvent.StartTime.Year != newEvent.EndTime.Year)
                isAllDayEvent = true;

            // usually 1day event is marked from today 00:00 to tomorrow 00:00; this is not technically correct
            if (startTime != endTime && endTime.Hour == 0 && endTime.Minute == 0 && endTime.Second == 0)
                endTime = endTime.AddMilliseconds(-1);

            double cellWidth = weekGrid.ColumnDefinitions[1].ActualWidth;
            double cellHeight = weekGrid.RowDefinitions[1].ActualHeight;

            #region determine event placement in the WeekGrid

            // adjust newEvent inside the dayBeginsSetting and dayEndsSetting
            DateTime dayBegin = new DateTime(startTime.Year, startTime.Month, startTime.Day, dayBeginsSetting, 0, 0);
            DateTime dayEnd = (new DateTime(endTime.Year, endTime.Month, endTime.Day, dayEndsSetting, 0, 0)).Add(new TimeSpan(0, 0, -1));
            DateTime eventStart = (startTime < dayBegin ? dayBegin : startTime);
            DateTime eventEnd = (endTime > dayEnd ? dayEnd : endTime);
            int eventDuration = Convert.ToInt32(((TimeSpan)(eventEnd - eventStart)).TotalMinutes);

            // 'continued' means the AllDay event started in previous week
            if (startTime < startWeekDay)
            {
                continued = true;
                startTime = eventStart = startWeekDay;
            }

            // skip events out of the grid (out of dayBeginsSetting-dayEndsSetting range)
            if (eventEnd < dayBegin || eventStart > dayEnd)
            {
                App.logger.AppendWarn("WGC.AddEvent(): skipped: event of {0} to {1} lies out of WeekGrid", newEvent.StartTime, newEvent.EndTime);
                return false;
            }

            if (eventDuration < 0)
            {
                App.logger.AppendWarn("WGC.AddEvent(): skipped: event of {0} has got negative duration", startTime);
                return false;
            }

            if (eventDuration < WEEKGRID_EVENT_MINDURATION)
            {
                eventDuration = WEEKGRID_EVENT_MINDURATION;
                eventEnd = eventEnd.AddMinutes(WEEKGRID_EVENT_MINDURATION);
                App.logger.AppendInfo("WGC.AddEvent(): WEEKGRID_EVENT_MINDURATION applied to appt of {0}", startTime);
            }

            int rowStart = isAllDayEvent ? 0 : eventStart.Hour - dayBeginsSetting;
            int rowEnd = isAllDayEvent ? 0 : eventEnd.Hour - dayBeginsSetting;

            #region uncomment to break to specific event for debugging
            //if (newEvent.Subject != null && newEvent.Subject.IndexOf("IT110029") > -1)
            //    Debugger.Break();
            #endregion

            // determine column; first WeekGrid grid contains Hours label (ie. 10AM)
            DateTime trimmedEventStart = App.DateTimeTrim(eventStart, TimeSpan.TicksPerDay);
            DateTime trimmedEventEnd = App.DateTimeTrim(eventEnd, TimeSpan.TicksPerDay);
            int col = Convert.ToInt32(new TimeSpan(trimmedEventStart.Ticks - startWeekDay.Ticks).TotalDays + 1);

            // skip events out of the grid (hidden columns ie. weekends)
            if (col > 7 || col < 1)
            {
                App.logger.AppendInfo("WGC.AddEvent(): skipped: event of {0} lies outside the week range", startTime);
                return false;
            }
            if (weekGrid.ColumnDefinitions[col].Width.Value == 0)
            {
                if (isAllDayEvent)
                    continued = true;
                else
                {
                    App.logger.AppendInfo("WGC.AddEvent(): skipped: event of {0} lies on an hidden (weekend) column", startTime);
                    return false;
                }
            }
            
            // determines event overlapping other events
            int overlapping = 0;
            foreach (WeekGridEvent weekGridEvent in weekGridEvents[col])
                if (weekGridEvent.IsAllDayEvent == isAllDayEvent)
                    if ((startTime >= weekGridEvent.StartTime && startTime < weekGridEvent.EndTime) ||
                         (endTime <= weekGridEvent.EndTime && endTime > weekGridEvent.StartTime))
                        overlapping = weekGridEvent.Overlapping + 1;

            double top = isAllDayEvent ? 2 : (cellHeight / 12) * (eventStart.Minute / 5);
            double bottom = isAllDayEvent ? 2 : (eventEnd.Minute == 0 ? cellHeight : cellHeight - ((cellHeight / 12) * (eventEnd.Minute / 5)));

            #endregion

            #region determine the subject handling null and similar

            string subject = "";
            if (newEvent.IsPrivate)
                subject = Strings.PrivateLabel;
            else
            {
                if (newEvent.Subject == null)
                    subject = Strings.NoTitleLabel;
                else
                    subject = newEvent.Subject;

                if (subject.Trim().Length == 0)
                    subject = Strings.NoTitleLabel;
            }

            #endregion

            #region draw the event

            SolidColorBrush accountBrush = GetAccountBrush(newEvent);

            AppointmentDrawHelper adh = new AppointmentDrawHelper();
            adh.IsPrivate = newEvent.IsPrivate;
            adh.BorderMargin = new Thickness(((overlapping % 2) * WEEKGRID_EVENT_OVERLAP_INDENTATION), top, 0, bottom);
            adh.BorderBrush = accountBrush;
            adh.Background = ColorExtensions.GetBlendedBrush(accountBrush, WEEKGRID_CELL_BACKGROUND_OPACITY);
            adh.StrokeThickness = (newEvent.Status == Microsoft.Phone.UserData.AppointmentStatus.Busy ? 5 : 2);
            adh.TextStyle = Application.Current.Resources[(isAllDayEvent ? "AllDayEventTextStyle" : (weekGrid.RowDefinitions[2].ActualHeight < WEEKGRID_CELL_TEXTSMALL_THRESHOLD ? "EventTextStyleSmall" : "EventTextStyle"))] as Style;
            adh.ArrowVisible = (continued ? Visibility.Visible : Visibility.Collapsed);
            adh.Subject = newEvent.Subject;
            adh.Location = newEvent.Location;

            Button bn = new Button();
            bn.DataContext = adh;
            bn.Template = (ControlTemplate)Application.Current.Resources["EventCell"];
            bn.Click += new RoutedEventHandler(eventButtonClick);
            bn.RenderTransform = new CompositeTransform();
            bn.RenderTransformOrigin = new Point(0.5, 0.5);

            #endregion
            
            // this holds the Appointment object to be used in Details.xaml
            bn.Tag = newEvent;

            // add event to WeekGrid
            Grid.SetRow(bn, (isAllDayEvent ? rowStart : rowStart + 1) );
            Grid.SetColumn(bn, col);
            Grid.SetRowSpan(bn, rowEnd - rowStart + 1);
            if (isAllDayEvent)
                Grid.SetColumnSpan(bn, Convert.ToInt32(new TimeSpan(trimmedEventEnd.Ticks - trimmedEventStart.Ticks).TotalDays + 1));
            weekGrid.Children.Add(bn);

            // add event to WeekGridEvent list, used to determine overlapped events
            WeekGridEvent newWeekGridEvent = new WeekGridEvent();
            newWeekGridEvent.StartTime = startTime;
            newWeekGridEvent.EndTime = endTime;
            newWeekGridEvent.GridEvent = bn;
            newWeekGridEvent.Overlapping = overlapping;
            newWeekGridEvent.IsAllDayEvent = isAllDayEvent;
            weekGridEvents[col].Add(newWeekGridEvent);

            App.logger.AppendInfo("WGC.AddEvent(): added: event of {0} to {1} continued: {2}", newEvent.StartTime, newEvent.EndTime, continued);

            return true;
        }

        protected SolidColorBrush GetAccountBrush(Appointment appt)
        {
#if !FAKE_APPOINTMENT

            // determines the brush to draw the event from the appSettings
            foreach (AccountSetting account in accountsSetting)
            {
                if (account.Kind == appt.Account.Kind.ToString())
                    if (account.Name == appt.Account.Name)
                    {
                        AccentColorNameToBrush ntBrush = new AccentColorNameToBrush();
                        return (SolidColorBrush)ntBrush.Convert(account.AccountColor, null, null, null);
                    }
            }
#endif

            Color color = (Color)Application.Current.Resources["PhoneAccentColor"];
            return new SolidColorBrush(color);
        }

        protected bool IsAccountEnabled(Appointment appt)
        {
#if !FAKE_APPOINTMENT

            if (!showPrivateSetting)
                if (appt.IsPrivate)
                    return false;

            Account account = appt.Account;

            foreach (AccountSetting accountSetting in accountsSetting)
                if (accountSetting.Kind == account.Kind.ToString())
                    if (accountSetting.Name == account.Name)
                        if (!accountSetting.Enabled)
                            return false;

#endif
            
            return true;
        }

        protected void Clear()
        {
            for (int i = weekGrid.Children.Count; i > 0; i--)
            {
                if (weekGrid.Children[i - 1].GetType() == typeof(Button))
                {
                    Button child = weekGrid.Children[i - 1] as Button;

                    if (child.Tag != null && child.Tag.GetType() == typeof(Appointment))
                    {
                        weekGrid.Children.RemoveAt(i - 1);
                        continue;
                    }
                }

                if (weekGrid.Children[i - 1].GetType() == typeof(TextBlock))
                {
                    TextBlock child = weekGrid.Children[i - 1] as TextBlock;

                    if (child.Tag != null && child.Tag.ToString() == "hour")
                        weekGrid.Children.RemoveAt(i - 1);
                }
            }
        }

        public void PopulateFirstColumn()
        {
            for (int i = WEEKGRID_ALLDAYROW_OFFSET; i < weekGrid.RowDefinitions.Count - 1; i++)
            {
                int HourToDisplay = dayBeginsSetting + i - WEEKGRID_ALLDAYROW_OFFSET;

                string designator = "";
                int hourTrim = 0;

                if (DateTimeFormatInfo.CurrentInfo.LongTimePattern.Contains("H"))
                    designator = "00";
                else
                {
                    designator = (HourToDisplay >= 12 ? "PM" : "AM");
                    hourTrim = (HourToDisplay > 12 ? 12 : 0);
                }

                Run block1 = new Run();
                block1.FontSize = 20;
                block1.Text = ((int)(HourToDisplay - hourTrim)).ToString();

                Run block2 = new Run();
                block2.FontSize = 15;
                block2.Text = designator;

                TextBlock tb = new TextBlock();
                tb.TextAlignment = TextAlignment.Right;
                tb.Inlines.Add(block1);
                tb.Inlines.Add(block2);
                tb.Tag = "hour";
                tb.Margin = new Thickness(0, 0, 2, 0);

                Grid.SetRow(tb, i);
                Grid.SetColumn(tb, 0);

                weekGrid.Children.Add(tb);
            }

        }

        public bool AdjustHeight(bool Zoom)
        {
            // handles DayBeginsSetting, DayEndsSetting and WeekGrid rows height
            // must be called before AdjustWidth()
                                                                                                                                                                                                                                                           
            // this is also in the constructor event and could be improved...
            AppSettings appSettings = new AppSettings();
            dayBeginsSetting = appSettings.DayBeginsSetting;
            dayEndsSetting = appSettings.DayEndsSetting;
            showWeekendSetting = appSettings.ShowWeekendSetting;

            int cellMinHeight = (Zoom ? 1 : WEEKGRID_CELL_MINHEIGHT);
            int containerHeight = Convert.ToInt32(((ScrollViewer)((Grid)weekGrid.Parent).Parent).ActualHeight);
            int rowsCount = dayEndsSetting - dayBeginsSetting + WEEKGRID_ALLDAYROW_OFFSET;
            int rowsVisible = containerHeight / cellMinHeight;
            int rowHeight = (rowsCount > rowsVisible ? cellMinHeight : (containerHeight / rowsCount));
            double oldHeigth = weekGrid.RowDefinitions[1].Height.Value;

            weekGrid.RowDefinitions[0].Height = new GridLength((rowHeight > WEEKGRID_ALLDAYROW_MINHEIGHT ? rowHeight : WEEKGRID_ALLDAYROW_MINHEIGHT) / 2.5);
            for (int row = 1; row < rowsCount; row++)
                weekGrid.RowDefinitions[row].Height = new GridLength(rowHeight);

            for (int row = rowsCount; row <= 23 + WEEKGRID_ALLDAYROW_OFFSET; row++)
                weekGrid.RowDefinitions[row].Height = new GridLength(0);

            return (oldHeigth != weekGrid.RowDefinitions[1].Height.Value);
        }

        public void AdjustWidth()
        {
            // handles ShowWeekendSetting and WeekGrid columns width
            // WeekGrid calculates visible columns width by itself
            if (showWeekendSetting)
            {
                weekGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                weekGrid.ColumnDefinitions[6].Width = new GridLength(1, GridUnitType.Star);
                weekGrid.ColumnDefinitions[7].Width = new GridLength(1, GridUnitType.Star);

                weekHeaderGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                weekHeaderGrid.ColumnDefinitions[6].Width = new GridLength(1, GridUnitType.Star);
                weekHeaderGrid.ColumnDefinitions[7].Width = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday)
                {
                    weekGrid.ColumnDefinitions[6].Width = new GridLength(0);
                    weekGrid.ColumnDefinitions[7].Width = new GridLength(0);

                    weekHeaderGrid.ColumnDefinitions[6].Width = new GridLength(0);
                    weekHeaderGrid.ColumnDefinitions[7].Width = new GridLength(0);
                }
                else
                {
                    weekGrid.ColumnDefinitions[1].Width = new GridLength(0);
                    weekGrid.ColumnDefinitions[7].Width = new GridLength(0);

                    weekHeaderGrid.ColumnDefinitions[1].Width = new GridLength(0);
                    weekHeaderGrid.ColumnDefinitions[7].Width = new GridLength(0);
                }
            }
        }
    }
}
