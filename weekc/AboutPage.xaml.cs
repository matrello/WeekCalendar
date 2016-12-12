using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using SlickThought.Phone;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using weekc.Languages;

namespace weekc
{
    public partial class AboutPage : PhoneApplicationPage
    {
        string version;

        public AboutPage()
        {
            InitializeComponent();

            version = App.GetAppVersion();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PhoneApplicationService.Current.State["fromSubPage"] = (bool)true;
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // http://blogs.infosupport.com/blogs/ernow/archive/2011/06/03/windows-phone-7-databinding-and-the-pivot-control.aspx
            VersionText.DataContext = null;
            VersionText.DataContext = version;

            TitleImage.Source = App.GetPageApplicationIcon();

            ImageBrush ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri(App.IsDark ? "/Icons/appbar.twitter.bird.dark.png" : "/Icons/appbar.twitter.bird.light.png", UriKind.Relative));
            ib.AlignmentX = AlignmentX.Left;
            ib.Stretch = Stretch.Uniform;
            TwitterButton.Background = ib;

            ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri(App.IsDark ? "/Icons/appbar.marketplace.dark.png" : "/Icons/appbar.marketplace.light.png", UriKind.Relative));
            ib.AlignmentX = AlignmentX.Left;
            ib.Stretch = Stretch.Uniform;
            StoreButton.Background = ib;

            if (TrialManager.Current.IsTrial())
            {
                if (TrialManager.Current.IsExpired)
                    TrialText.Text = Strings.TrialExpired;
                else
                    TrialText.Text = Strings.TrialText;
            }
            else
                TrialText.Text = Strings.TrialLicensed;
 
            CreditsText.Text = Strings.CopyrightLabel;
            if (Strings.TranslationLabel.Length > 0)
                CreditsText.Text += '\n' + Strings.TranslationLabel;
            CreditsText.Text += '\n';
        }

        private void Store_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.Show();
        }

        private void Twitter_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask ie = new WebBrowserTask();
            ie.Uri = new Uri("http://mobile.twitter.com/WeekCalendarWP");
            ie.Show();
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "WeekCalendar@live.com";
            emailComposeTask.Body = "Week Calendar version " + version + "\n\n";
            emailComposeTask.Subject = "Week Calendar version " + version;
            emailComposeTask.Show();
        }

    }
}