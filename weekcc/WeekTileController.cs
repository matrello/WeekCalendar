using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTools;
using ImageTools.IO.Png;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;

namespace weekcc
{
    public class WeekTileController
    {
        Random rnd;

        private bool tileReady;

        private int dayBeginsSetting;
        private int dayEndsSetting;
        private bool showWeekendSetting;
        private bool showWeekNumberSetting;
        private bool showPrivateSetting;
        private List<AccountSetting> accountsSetting;

        const string tileFileName = "/Shared/ShellContent/weekc_tile.jpg";
        const int WEEKGRID_EVENT_MINDURATION = 15;
        int tile_ColumnWidth, tile_MarginLeft, tile_MarginTop;
        int tile_EventsMarginLeft, tile_EventsMarginTop, tile_EventsHeigth;
        int tile_DayMarkerMarginTop;

        private DateTime startWeekDay = DateTime.MinValue;
        private List<Appointment> weekAppts = new List<Appointment>();  // returned from WP7 or simulated on WP Emulator
        private Logger logger;
        private string title, shortTitle;

        public WeekTileController(Logger logger, string title, string shortTitle)
        {
            rnd = new Random();
            accountsSetting = new List<AccountSetting>();
            AppSettings appSettings = new AppSettings();
            dayBeginsSetting = appSettings.DayBeginsSetting;
            dayEndsSetting = appSettings.DayEndsSetting;
            showWeekendSetting = appSettings.ShowWeekendSetting;
            showWeekNumberSetting = appSettings.ShowWeekNumberSetting;
            showPrivateSetting = appSettings.ShowPrivateSetting;

            foreach (AccountSetting account in appSettings.AccountsSetting)
                accountsSetting.Add(account);

            this.logger = logger;
            this.title = title;
            this.shortTitle = shortTitle;

            if (showWeekendSetting)
            {
                tile_ColumnWidth = 19;
                tile_MarginLeft = 19;
                tile_MarginTop = 40;
                tile_EventsMarginLeft = 23;
                tile_EventsMarginTop = 57;
                tile_EventsHeigth = 63;
                tile_DayMarkerMarginTop = 39;
            }
            else
            {
                tile_ColumnWidth = 22;
                tile_MarginLeft = 31;
                tile_MarginTop = 40;
                tile_EventsMarginLeft = 35;
                tile_EventsMarginTop = 57;
                tile_EventsHeigth = 63;
                tile_DayMarkerMarginTop = 39;
            }

            ImageTools.IO.Encoders.AddEncoder<ImageTools.IO.Png.PngEncoder>();
            WeekToday();
        }

        public void WeekToday()
        {
            int delta = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek - DateTime.Now.DayOfWeek;
            startWeekDay = DateTimeTrim(DateTime.Now.AddDays(delta), TimeSpan.TicksPerDay);
        }

        public void TileRefresh(bool wait)
        {
            tileReady = false;
            Appointments appts = new Appointments();
            appts.SearchCompleted += new EventHandler<AppointmentsSearchEventArgs>(appts_SearchCompleted);

#if DEBUG_TOASTS
            ShellToast toast = new ShellToast();
            toast.Title = "PeriodicTask test";
            toast.Content = "006 OK";
            toast.Show();
#endif

            logger.AppendInfo("WTC.TileRefresh(): begin appts searching from {0}", startWeekDay.ToString());
            appts.SearchAsync(startWeekDay, startWeekDay.AddDays(7), "WeekTile");

            if (wait)
            {
                while (!tileReady)
                    System.Threading.Thread.Sleep(10);
            }
        }

        private WriteableBitmap DrawTile()
        {
            WriteableBitmap bmp = new WriteableBitmap(173, 173);

            var logo = new BitmapImage(new Uri((showWeekendSetting ? "/Assets/Tiles/BackgroundWE.png" : "/Assets/Tiles/BackgroundNWE.png"), UriKind.Relative));
            var img = new Image { Source = logo };
            logo.CreateOptions = BitmapCreateOptions.None;
            bmp.Render(img, null);

            var tt = new TranslateTransform { X = tile_MarginLeft, Y = tile_MarginTop };

            DateTime day = startWeekDay;
            for(int i=0; i<7; i++)
            {
                if (!IsWeekDay(i))
                {
                    if (day.Day == DateTime.Now.Day)
                    {
                        // draws the today marker
                        System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                        rect.Height = 2;
                        rect.Width = tile_ColumnWidth - 8;
                        rect.StrokeDashCap = PenLineCap.Square;
                        rect.Stroke = new SolidColorBrush(Colors.White);

                        var tt2 = new TranslateTransform { X = tt.X + 4, Y = tile_DayMarkerMarginTop };
                        bmp.Render(rect, tt2);
                    }

                    bmp.Render(DrawHeadDay(day.ToString("ddd")), tt);
                    tt.X += tile_ColumnWidth;
                }

                day = day.AddDays(1);
            }

            tt.Y += 8;
            for (int i = 0; i < (showWeekendSetting ? 6 : 4); i++)
            {
                // draws vertical day separators lines
                System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
                rect.Height = 70;
                rect.Width = 1;
                rect.StrokeDashArray = new DoubleCollection { 2, 2 };
                rect.StrokeDashCap = PenLineCap.Square;
                rect.Stroke = new SolidColorBrush(Colors.White);

                tt.X = tile_MarginLeft + tile_ColumnWidth + (i * tile_ColumnWidth);
                bmp.Render(rect, tt);
            }

            bmp.Invalidate();

            return bmp;
        }

        private TextBlock DrawHeadDay(string dayOfWeek)
        {
            var tb = new TextBlock();
            tb.Foreground = new SolidColorBrush(Colors.White);
            tb.FontSize = 13.0;
            tb.Width = tile_ColumnWidth;
            tb.TextAlignment = TextAlignment.Center;
            tb.Text = dayOfWeek.ToString().Substring(0, 1).ToLower();

            return tb;
        }

        public void AddEvent(WriteableBitmap bmpTile, Appointment newEvent)
        {

            // WeekTile doesn't show AllDay events
            if (newEvent.IsAllDayEvent)
                return;

            // events that span across more days are treated as AllDay events
            if (newEvent.StartTime.Day != newEvent.EndTime.Day || newEvent.StartTime.Month != newEvent.EndTime.Month || newEvent.StartTime.Year != newEvent.EndTime.Year)
                return;

            DateTime startTime = newEvent.StartTime;
            DateTime endTime = newEvent.EndTime;          

            #region determine event placement in the WeekTile

            // adjust newEvent inside the dayBeginsSetting and dayEndsSetting
            DateTime dayBegin = new DateTime(startTime.Year, startTime.Month, startTime.Day, dayBeginsSetting, 0, 0);
            DateTime dayEnd = (new DateTime(endTime.Year, endTime.Month, endTime.Day, dayEndsSetting, 0, 0)).Add(new TimeSpan(0, 0, -1));
            DateTime eventStart = (startTime < dayBegin ? dayBegin : startTime);
            DateTime eventEnd = (endTime > dayEnd ? dayEnd : endTime);
            double eventDuration = ((TimeSpan)(eventEnd - eventStart)).TotalMinutes;
            double minuteHeight = (double)tile_EventsHeigth / ((dayEndsSetting - dayBeginsSetting) * 60);
            double eventTop = ((TimeSpan)(eventStart - dayBegin)).TotalMinutes * minuteHeight;

            // skip events out of the grid (out of dayBeginsSetting-dayEndsSetting range)
            if (eventEnd < dayBegin || eventStart > dayEnd)
            {
                logger.AppendWarn("WTC.DrawEvent(): skipped: event of {0} lies out of WeekGrid", startTime);
                return;
            }

            if (eventDuration < 0)
            {
                logger.AppendWarn("WTC.DrawEvent(): skipped: event of {0} has got negative duration", startTime);
                return;
            }

            if (eventDuration < WEEKGRID_EVENT_MINDURATION)
            {
                eventDuration = WEEKGRID_EVENT_MINDURATION;
                eventEnd.AddMinutes(WEEKGRID_EVENT_MINDURATION);
                logger.AppendInfo("WTC.DrawEvent(): WEEKGRID_EVENT_MINDURATION applied to appt of {0}", startTime);
            }

            DateTime trimmedEventStart = DateTimeTrim(startTime, TimeSpan.TicksPerDay);
            int col = Convert.ToInt32(new TimeSpan(trimmedEventStart.Ticks - startWeekDay.Ticks).TotalDays);

            if (!showWeekendSetting)
            {
                if (IsWeekDay(col))
                {
                    logger.AppendInfo("WTC.DrawEvent(): skipped: event of {0} lies on an hidden (weekend) column", startTime);
                    return;
                }
                else
                {
                    if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Sunday)
                        col -= 1;
                }
            }

            // skip events out of the grid (hidden columns ie. weekends)
            if (col > (showWeekendSetting ? 6 : 4) || col < 0)
            {
                logger.AppendInfo("WTC.DrawEvent(): skipped: event of {0} lies outside the week range", startTime);
                return;
            }

            #endregion

            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
            rect.Height = eventDuration * minuteHeight;
            rect.Width = tile_ColumnWidth - 8;
            rect.Stroke = new SolidColorBrush(Colors.White);
            rect.StrokeDashCap = PenLineCap.Square;
            rect.Fill = new SolidColorBrush(Colors.White);

            var tt = new TranslateTransform { X = tile_EventsMarginLeft + (col * tile_ColumnWidth), Y = tile_EventsMarginTop + eventTop };

            bmpTile.Render(rect, tt);

            logger.AppendInfo("WTC.DrawEvent(): added: event of {0}", startTime);

            return;
        }

        private bool IsWeekDay(int day)
        {
            bool isWeekDay = false;

            if (!showWeekendSetting)
            {
                if (CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday)
                {
                    if (day == 5 || day == 6)
                        isWeekDay = true;
                }
                else
                {
                    if (day == 0 || day == 6)
                        isWeekDay = true;
                }
            }

            return isWeekDay;
        }

        public void appts_SearchCompleted(object sender, AppointmentsSearchEventArgs e)
        {

            #region collecting Appointments

            logger.AppendInfo("WTC.appts_SearchCompleted(): from {0} to {1}", e.StartTimeInclusive.ToString(), e.EndTimeInclusive.ToString());
            weekAppts.Clear();

#if FAKE_APPOINTMENT

            // this is used to simulate the presence of Appointments on WP7 emulator
            for(int k = 0; k<6; k++)
            {
                string[] locations = { "Roma", "Zug", "Zurigo", "Ginevra", "Grottammare", "Benevento", "Monteroni" };
                DateTime startd = startWeekDay.AddDays(rnd.Next(0, 7));
                DateTime startd1 = startd.AddDays(0);

                int starth = rnd.Next(0, 3) + 9;
                int endh = starth + rnd.Next(2, 3);
                int startm = 30 * rnd.Next(0, 2);
                int endm = 30 * rnd.Next(0, 2);
                int locationn = rnd.Next(0, 7);
                int status = rnd.Next(0, 9);

                //startd1 = startd.AddDays(1);
                //starth = 15;
                //endh = 16;

                Appointment cappt = new Appointment();
                cappt.StartTime = new DateTime(startd.Year, startd.Month, startd.Day, starth, startm, 00);
                cappt.EndTime = new DateTime(startd1.Year, startd1.Month, startd1.Day, endh, endm, 00);
                cappt.Subject = "Random event " + k.ToString() ;
                cappt.Details = "Dictumst eleifend facilisi faucibus, dictumst eleifend facilisi faucibus.";
                cappt.Location = locations[locationn];
                cappt.Status = (status == 2 ? AppointmentStatus.Tentative : AppointmentStatus.Busy);
                weekAppts.Add(cappt);
            }

#else

    #if DEBUG_TOASTS
            ShellToast toast = new ShellToast();
            toast.Title = "PeriodicTask test";
            toast.Content = "007 OK";
            toast.Show();
    #endif

            foreach (Appointment appt in e.Results)
                weekAppts.Add(appt);
#endif

            logger.AppendInfo("WTC.appts_SearchCompleted(): retrieved {0} appts", weekAppts.Count);

            #endregion

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
#if DEBUG_TOASTS
                ShellToast toast2 = new ShellToast();
                toast2.Title = "PeriodicTask test";
                toast2.Content = "008 OK";
                toast2.Show();
#endif

                WriteableBitmap bmpTile = DrawTile();

#if FAKE_APPOINTMENT
                foreach (Appointment appt in weekAppts)
                    AddEvent(bmpTile, appt);
#else
                foreach (Appointment appt in weekAppts)
                    if (IsAccountEnabled(appt))
                        AddEvent(bmpTile, appt);
#endif

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    ExtendedImage imgTile = ImageExtensions.ToImage(bmpTile);
                    PngEncoder enc = new PngEncoder();

                    using (var st = new IsolatedStorageFileStream(tileFileName, FileMode.Create, FileAccess.Write, store))
                        enc.Encode(imgTile, st);
                }

#if DEBUG_TOASTS
                toast2 = new ShellToast();
                toast2.Title = "PeriodicTask test";
                toast2.Content = "009 OK";
                toast2.Show();
#endif

                int weekNumber = 0;

                if (showWeekNumberSetting)
                {
                    var systemCalendar = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar;
                    weekNumber = systemCalendar.GetWeekOfYear(
                        startWeekDay,
                        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.CalendarWeekRule,
                        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
                }

                ShellTile pinnedTile = ShellTile.ActiveTiles.First();
                Uri backgroundImageUri = new Uri(string.Format("isostore:{0}", tileFileName), UriKind.Absolute);

#if WP8
                var tile = new FlipTileData();
                tile.SmallBackgroundImage = backgroundImageUri;
#else
                var tile = new StandardTileData();
#endif
                tile.BackgroundImage = backgroundImageUri;
                tile.Title = string.Format("{0} {1}{2}-{3}", (showWeekNumberSetting ? shortTitle : title),
                    (showWeekNumberSetting ? weekNumber + ": " : ""), startWeekDay.Day, startWeekDay.AddDays(6).Day);

                pinnedTile.Update(tile);

#if DEBUG_TOASTS
                toast2 = new ShellToast();
                toast2.Title = "PeriodicTask test";
                toast2.Content = "010 OK";
                toast2.Show();
#endif

                tileReady = true;
            });

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


        public static DateTime DateTimeTrim(DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks);
        }

    }

}
