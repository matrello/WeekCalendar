using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.Phone.UserData;
using weekcc;
using weekc.Languages;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;

namespace weekc
{
    public partial class Search : PhoneApplicationPage
    {
        const double BORDERAPPTS_HEIGHT_MARGIN = 100;

        public ObservableCollection<Appointment> WeekAppts { get { return weekAppts; } }
        private ObservableCollection<Appointment> weekAppts = new ObservableCollection<Appointment>();  // returned from WP7 or simulated on WP Emulator
        private ProgressIndicator _progressIndicator;

        ApplicationBarIconButton searchButton;

        NavigationMode navigationMode;
        bool navigationFromSubPage;

        public Search()
        {
            InitializeComponent();
            AccountsList.SummaryForSelectedItemsDelegate = Summarize;
            AccountsList.Items.Clear();
            AccountsList.SelectedItems = new ObservableCollection<object>();
            var phoneAccounts = new Microsoft.Phone.UserData.Appointments().Accounts;
            foreach (var phoneAccount in phoneAccounts)
            {
                AccountsList.Items.Add(phoneAccount.Name);
                AccountsList.SelectedItems.Add(phoneAccount.Name);
            }
            ListBoxAppts.DataContext = this;
            SearchRangeList.Items.Add(Strings.SearchRangeLast3);
            SearchRangeList.Items.Add(Strings.SearchRangeThisMonth);
            SearchRangeList.Items.Add(Strings.SearchRangeNext3);
            SearchRangeList.SelectedIndex = 1;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            navigationMode = e.NavigationMode;
            navigationFromSubPage = false;
            if (PhoneApplicationService.Current.State.ContainsKey("fromSubPage"))
            {
                navigationFromSubPage = (bool)PhoneApplicationService.Current.State["fromSubPage"];
                PhoneApplicationService.Current.State["fromSubPage"] = (bool)false;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            #region init ApplicationBar

            if (ApplicationBar.Buttons.Count == 0)
            {
                searchButton = new ApplicationBarIconButton(new Uri("/Icons/appbar.feature.search.rest.png", UriKind.Relative));
                searchButton.Text = Strings.SearchTitle;
                searchButton.Click += new EventHandler(searchButton_Click);
                searchButton.IsEnabled = false;

                ApplicationBar.Buttons.Add(searchButton);
            }
            
            #endregion

            if (!navigationFromSubPage)
            {
                weekAppts.Clear();
                TextNoAppts.Text = Strings.SearchDefaultText;
                BorderAppts.Visibility = System.Windows.Visibility.Collapsed;
                BorderNoAppts.Visibility = System.Windows.Visibility.Visible;

                searchButton.IsEnabled = false;
                SearchPanel.Visibility = System.Windows.Visibility.Visible;

                if (navigationMode == NavigationMode.New)
                    SearchPanelDown.Begin();
                else
                    SearchTextBox.Focus();
            }

            TitleImage.Source = App.GetPageApplicationIcon();
        }

        void searchButton_Click(object sender, EventArgs e)
        {
            searchButton.IsEnabled = false;
            SearchPanel.Visibility = System.Windows.Visibility.Visible;
            SearchPanelDown.Begin();
        }

        private string Summarize(IList items)
        {
            string str = "";
            if (null != items)
                foreach (string item in items)
                    str += (str.Length > 1 ? ", " : "") + item;

            return str;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchTextBox.Text.Length > 1)
            {
                SearchPanelUp.Begin();

                _progressIndicator = new ProgressIndicator();
                _progressIndicator.IsVisible = true;
                _progressIndicator.IsIndeterminate = true;
                SystemTray.ProgressIndicator = _progressIndicator;

                Microsoft.Phone.UserData.Appointments appts = new Microsoft.Phone.UserData.Appointments();
                appts.SearchCompleted += new EventHandler<Microsoft.Phone.UserData.AppointmentsSearchEventArgs>(appts_SearchCompleted);

                DateTime startTime = DateTime.Now;
                DateTime endTime = DateTime.Now;
                switch (SearchRangeList.SelectedIndex)
                {
                    case 0:
                        startTime = DateTime.Now.AddMonths(-3);
                        endTime = DateTime.Now;
                        break;
                    case 1:
                        startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1).AddMilliseconds(-1);
                        break;
                    case 2:
                        startTime = DateTime.Now;
                        endTime = DateTime.Now.AddMonths(3);
                        break;
                }

                appts.SearchAsync(startTime, endTime, int.MaxValue, "WeekGrid");
                TextNoAppts.Text = "";
            }
        }

        void appts_SearchCompleted(object sender, Microsoft.Phone.UserData.AppointmentsSearchEventArgs e)
        {
            weekAppts.Clear();

#if FAKE_APPOINTMENT

            // this is used to simulate the presence of Appointments on WP7 emulator
            for (int k = 0; k < 8; k++)
            {
                string[] locations = { "Roma", "Zug", "Zurich", "Benevento", "Bruxelles", "Ponte Galeria", "Muratella" };
                string[] subjects = { "Geek time", "Play with WP7", "WC demo meeting", "Children babysitting", "Meet with Ilaria", "Meet with Alessia", "Meet with my wife" };
                DateTime startd = DateTime.Now.AddDays(App.rnd.Next(0, 7));
                DateTime startd1 = startd;

                int starth = App.rnd.Next(0, 5) + 9;
                int endh = starth + App.rnd.Next(2, 4);
                int startm = 30 * App.rnd.Next(0, 2);
                int endm = 30 * App.rnd.Next(0, 2);
                int locationn = App.rnd.Next(0, 7);
                int status = App.rnd.Next(0, 9);
                int subjectn = App.rnd.Next(0, 7);
                int meeting = App.rnd.Next(0, 2);
                int allday = App.rnd.Next(0, 3);

                Appointment cappt = new Appointment();
                if (allday == 1)
                {
                    cappt.IsAllDayEvent = true;
                    startd1 = startd.AddDays(App.rnd.Next(0, 3) + 1);
                    starth = startm = endh = endm = 0;
                }

                cappt.StartTime = new DateTime(startd.Year, startd.Month, startd.Day, starth, startm, 00);
                cappt.EndTime = new DateTime(startd1.Year, startd1.Month, startd1.Day, endh, endm, 00);
                cappt.Subject = subjects[subjectn];
                cappt.Details = "Dictumst eleifend facilisi faucibus, dictumst eleifend facilisi faucibus.";
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Details += cappt.Details;
                cappt.Location = cappt.StartTime.ToShortDateString() + " " + cappt.EndTime.ToShortDateString(); // locations[locationn];
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
            {
                if (!appt.IsPrivate)
                {
                    if (IsAccountEnabled(appt) && appt.Subject.ToLower().IndexOf(SearchTextBox.Text.ToLower()) > -1)
                        weekAppts.Add(appt);
                }
            }
#endif

            if (weekAppts.Count == 0)
            {
                TextNoAppts.Text = Strings.SearchNoAppts;
                BorderAppts.Visibility = System.Windows.Visibility.Collapsed;
                BorderNoAppts.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                BorderAppts.Visibility = System.Windows.Visibility.Visible;
                BorderNoAppts.Visibility = System.Windows.Visibility.Collapsed;
            }

            _progressIndicator.IsIndeterminate = false;
        }

        protected bool IsAccountEnabled(Appointment appt)
        {
#if !FAKE_APPOINTMENT

            Account account = appt.Account;

            foreach (string accountName in AccountsList.SelectedItems)
                if (accountName == account.Name)
                    return true;

            return false;
#else
            return true;
#endif
        }

        private void SearchPanelDown_Completed(object sender, EventArgs e)
        {
            SearchTextBox.Focus();
        }

        private void SearchPanelUp_Completed(object sender, EventArgs e)
        {
            SearchTextBox.IsEnabled = false;
            SearchTextBox.IsEnabled = true;
            searchButton.IsEnabled = true;

            SearchPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void PhoneApplicationPage_LayoutUpdated(object sender, EventArgs e)
        {
            BorderAppts.Height = Application.Current.Host.Content.ActualHeight - GridAppts.RowDefinitions[0].ActualHeight - BORDERAPPTS_HEIGHT_MARGIN;
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (SearchPanel.Visibility == Visibility.Visible)
            {
                SearchPanelUp.Begin();
                e.Cancel = true;
            }
        }

        private void ListBoxAppts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ListBox)sender).SelectedValue != null)
            {
                App.SelectedAppointment = ((ListBox)sender).SelectedValue as Appointment;
                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
                ListBoxAppts.SelectedIndex = -1;
            }
        }

    }

    // this class serves TextBlock to render the Account info
    public class AccountConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Appointment appt = (Appointment)value;

#if FAKE_APPOINTMENT
            return "Google - Outlook";
#else
            return string.Format("{0} - {1}", appt.Account.Name, appt.Account.Kind.ToString());
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}