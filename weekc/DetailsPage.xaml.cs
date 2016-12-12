using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using weekc.Languages;

namespace weekc
{
    // FM.2016.12.12 - released to the public. https://github.com/matrello/WeekCalendar

    public partial class DetailsPage : PhoneApplicationPage
    {
        public DetailsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PhoneApplicationService.Current.State["fromSubPage"] = (bool) true;
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // http://blogs.infosupport.com/blogs/ernow/archive/2011/06/03/windows-phone-7-databinding-and-the-pivot-control.aspx
            this.DataContext = null;
            this.DataContext = App.SelectedAppointment;
        }

        private void DetailsPivot_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.SelectedAppointment.Attendees == null)
            {
                TitleText.Text = Strings.DetailsTitleAppointment;
                DetailsPivot.Items.Remove(DetailsPivot.Items.Single(p => ((PivotItem)p).Name == "PivotItemAttendees"));
            }
            else
            {

                if (App.SelectedAppointment.Attendees.Cast<Attendee>().Count() == 0)
                {
                    TitleText.Text = Strings.DetailsTitleAppointment;
                    DetailsPivot.Items.Remove(DetailsPivot.Items.Single(p => ((PivotItem)p).Name == "PivotItemAttendees"));
                }
                else
                    TitleText.Text = Strings.DetailsTitleMeeting;
            }

#if FAKE_APPOINTMENT
            AccountInfo.Text = "Google - Outlook";
#else
            AccountInfo.Text = string.Format("{0} - {1}", App.SelectedAppointment.Account.Name, App.SelectedAppointment.Account.Kind.ToString());
#endif

        }

        private void ListBoxAttendees_Loaded(object sender, RoutedEventArgs e)
        {
            Contacts cons;
            ListBox listBox = sender as ListBox;

            // loads the listboxes Attendees with Contact images
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)(ListBoxAttendees.ItemContainerGenerator.ContainerFromIndex(i));
                Image image = FindFirstElementInVisualTree<Image>(item);
                Attendee attendee = listBox.Items[i] as Attendee;

                cons = new Contacts();
                cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);
                cons.SearchAsync(attendee.EmailAddress, FilterKind.EmailAddress, image);
            }

            if (listBox.Items.Count > 0)
            {
                // loads the organizer Attendee with Contact image
                cons = new Contacts();
                cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);
                cons.SearchAsync(App.SelectedAppointment.Organizer.EmailAddress, FilterKind.EmailAddress, OrganizerImage);
            }

        }

        public void Contacts_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            Image image = e.State as Image;

            foreach (Contact contact in e.Results)
            {
                System.IO.Stream imageStream = contact.GetPicture();
                if (null != imageStream)
                {
                    image.Source = Microsoft.Phone.PictureDecoder.DecodeJpeg(imageStream);
                }
            }

            if (image.Source == null)
            {
                BitmapImage bit = new BitmapImage();
                bit.SetSource(App.GetResourceStream(new Uri("Icons/anonymous.png", UriKind.Relative)).Stream);
                image.Source = bit;
            }
        }

        private T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            // http://www.windowsphonegeek.com/tips/how-to-access-a-control-placed-inside-listbox-itemtemplate-in-wp7
            var count = VisualTreeHelper.GetChildrenCount(parentElement);
            if (count == 0)
                return null;

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parentElement, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var result = FindFirstElementInVisualTree<T>(child);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        #region http://blogs.msdn.com/b/mikeormond/archive/2010/12/16/displaying-html-content-in-windows-phone-7.aspx

        private string FetchBackgroundColor()
        {
            return IsBackgroundBlack() ? "#000;" : "#fff";
        }

        private string FetchFontColor()
        {
            return IsBackgroundBlack() ? "#fff;" : "#000";
        }

        private static bool IsBackgroundBlack()
        {
            return FetchBackGroundColor() == "#FF000000";
        }

        private static string FetchBackGroundColor()
        {
            string color;
            Color mc =
              (Color)Application.Current.Resources["PhoneBackgroundColor"];
            color = mc.ToString();
            return color;
        }

        private void WebBrowser1_Loaded(object sender, RoutedEventArgs e)
        {
            string fontColor = FetchFontColor();
            string backgroundColor = FetchBackgroundColor();

            SetBackground();

            // uncomment to generate a crash for testing problem reporting!
            //string gg = App.SelectedAppointment.Details.Substring(500, 1);

            var html = "<p>";
            if (App.SelectedAppointment.Details != null)
            {
                int pos = App.SelectedAppointment.Details.IndexOf("*~*~*~*~*~*~*~*~*~*");
                if (pos > -1)
                {
                    pos += 19;

                    if (pos < App.SelectedAppointment.Details.Length)
                    {

                        while (App.SelectedAppointment.Details.Substring(pos, 1) == "\n" || App.SelectedAppointment.Details.Substring(pos, 1) == "\r")
                        {
                            pos++;

                            if (pos >= App.SelectedAppointment.Details.Length)
                                break;
                        }

                        if (pos < App.SelectedAppointment.Details.Length)
                            html += App.SelectedAppointment.Details.Substring(pos);
                    }
                }
                else
                    html += App.SelectedAppointment.Details;
            }
            html += "</p>";

            var htmlScript = "<script>function getDocHeight() { " +
              "return document.getElementById('pageWrapper').offsetHeight;" +
              "}" +
              "function SendDataToPhoneApp() {" +
              "window.external.Notify('' + getDocHeight());" +
              "}</script>";

            var htmlConcat = string.Format("<html><head>{0}</head>" +
              "<body style=\"margin:0px;padding:0px;background-color:{3};\" " +
              "onLoad=\"SendDataToPhoneApp()\">" +
              "<div id=\"pageWrapper\" style=\"width:100%; color:{2}; " +
              "background-color:{3}\">{1}</div></body></html>",
              htmlScript,
              HtmlSanitize(html),
              fontColor,
              backgroundColor);

            WebBrowser1.NavigateToString(htmlConcat);
            WebBrowser1.IsScriptEnabled = true;
            WebBrowser1.ScriptNotify += new EventHandler<NotifyEventArgs>(WebBrowser1_ScriptNotify);
        }

        private void SetBackground()
        {
            Color mc =
              (Color)Application.Current.Resources["PhoneBackgroundColor"];
            WebBrowser1.Background = new SolidColorBrush(mc);

        }

        private void WebBrowser1_ScriptNotify(object sender, NotifyEventArgs e)
        {  // The browser is zooming the text so we need to
            // reduce the pixel size by the zoom level...
            // Which is about 0.50
            // removed: it resizes as its best

            // Show it once rendered
            WebBrowser1.Visibility = System.Windows.Visibility.Visible;
        }

        //http://stackoverflow.com/questions/2015563/replace-newlines-with-p-paragraph-and-with-br-tags (mostly)
        public static string HtmlSanitize(string input)
        {
            // Remove HTML tags
            string returnString = Regex.Replace(input, "< .*?>", "");

            // Decode HTML entities
            returnString = HttpUtility.HtmlDecode(returnString);

            // Decode NewLine
            returnString = "<p>" + returnString
                    .Replace(Environment.NewLine + Environment.NewLine, "</p><p>")
                    .Replace(Environment.NewLine, "<br />")
                    .Replace("</p><p>", "</p>" + Environment.NewLine + "<p>") + "</p>";

            return returnString;
        }
        #endregion

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            TitleImage.Source = App.GetPageApplicationIcon();
        }
    }

    // this class serves TextBlock to render StartTime and EndTime in a more readable format
    public class StartEndTimeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";

            Appointment appt = value as Appointment;
            DateTime startTime = appt.StartTime;
            DateTime endTime = appt.EndTime;
            bool isAllDayEvent = appt.IsAllDayEvent;
            string apptSchedule;

            // usually 1day event is market from today 00:00 to tomorrow 00:00; this is not technically true
            if (startTime != endTime && endTime.Hour == 0 && endTime.Minute == 0 && endTime.Second == 0)
                endTime = endTime.AddMilliseconds(-1);

            if (appt.IsAllDayEvent)
            {
                if (((TimeSpan)(endTime - startTime)).TotalDays < 1)
                    apptSchedule = string.Format("{0} ({1})",
                        startTime.ToString("d"), Strings.AllDayLabel);
                else
                    apptSchedule = string.Format("{0} - {1}",
                        startTime.ToString("d"), endTime.ToString("d"));
            }
            else
            {
                if (((TimeSpan)(endTime - startTime)).TotalDays < 1)
                    apptSchedule = string.Format("{0}, {1} - {2}",
                        startTime.ToString("d"), startTime.ToString("t"), endTime.ToString("t"));
                else
                    apptSchedule = string.Format("{0}, {1} - {2}, {3}",
                        startTime.ToString("d"), startTime.ToString("t"), endTime.ToString("d"), endTime.ToString("t"));
            }

            return apptSchedule;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // this class serves TextBlock to render the Subject handling null (private) or empty case
    public class SubjectConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Appointment appt = (Appointment)value;

            if (appt.IsPrivate)
                return Strings.PrivateLabel;

            if (appt.Subject == null)
                return Strings.NoTitleLabel;

            string subject = (string)appt.Subject;

            if (subject.Trim().Length == 0)
                return Strings.NoTitleLabel;

            return subject;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}