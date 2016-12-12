using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using weekc.Languages;
using weekcc;
using System.Windows.Media.Imaging;

namespace weekc
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void showWeekendSetting_Checked(object sender, RoutedEventArgs e)
        {
            showWeekendSetting.Content = Strings.On;
        }

        private void showWeekendSetting_Unchecked(object sender, RoutedEventArgs e)
        {
            showWeekendSetting.Content = Strings.Off;
        }

        private void showWeekNumberSetting_Checked(object sender, RoutedEventArgs e)
        {
            showWeekNumberSetting.Content = Strings.On;
        }

        private void showWeekNumberSetting_Unchecked(object sender, RoutedEventArgs e)
        {
            showWeekNumberSetting.Content = Strings.Off;
        }

        private void showPrivateSetting_Checked(object sender, RoutedEventArgs e)
        {
            showPrivateSetting.Content = Strings.On;
        }

        private void showPrivateSetting_Unchecked(object sender, RoutedEventArgs e)
        {
            showPrivateSetting.Content = Strings.Off;
        }

        private void zoomOnStartSetting_Checked(object sender, RoutedEventArgs e)
        {
            zoomOnStartSetting.Content = Strings.On;
        }

        private void zoomOnStartSetting_Unchecked(object sender, RoutedEventArgs e)
        {
            zoomOnStartSetting.Content = Strings.Off;
        }

        private void dayBeginsEndsSetting_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            if (dayBeginsSetting != null && dayEndsSetting != null)
            {
                if (dayBeginsSetting.Value > dayEndsSetting.Value || ( (dayEndsSetting.Value.Value.Hour - dayBeginsSetting.Value.Value.Hour) > 20) )
                {
                    AppSettings appSettings = new AppSettings();
                    appSettings.SetDayBeginsEndsDefaults();

                    dayBeginsSetting.Value = appSettings.DayBeginsSettingAsDate;
                    dayEndsSetting.Value = appSettings.DayEndsSettingAsDate;
                }
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            TitleImage.Source = App.GetPageApplicationIcon();
        }

    }

    // http://www.csharpblog.co.cc/2011/09/localize-toggleswitch-in-silverlight.html
    public class BoolToSwitchConverter : IValueConverter
    {
        private string FalseValue = Strings.Off;
        private string TrueValue = Strings.On;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return FalseValue;

            string v = value.ToString();

            if (v == "No" | v == "Off" | v == "Aus" | v == "Désactivé" )
                return FalseValue;

            return TrueValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null ? value.Equals(TrueValue) : false;
        }
    }

    /// <summary>
    /// A converter that takes a name of an accent color and returns a SolidColorBrush.
    /// PhoneToolkitSample.Data November 2011
    /// </summary>
    public class AccentColorNameToBrush : IValueConverter
    {
        /// <summary>
        /// Converts a name of an accent color to a SolidColorBrush.
        /// </summary>
        /// <param name="value">The accent color as a string.</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">The parameter</param>
        /// <param name="culture">The culture</param>
        /// <returns>A SolidColorBrush representing the accent color.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "By design")]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush brush = null;

            string v = value as string;
            if (null == v)
            {
                throw new ArgumentNullException("value");
            }

            if (v == "accent color")
            {
                Color color = (Color)Application.Current.Resources["PhoneAccentColor"];
                brush = new SolidColorBrush(color);
            }
            else
            {
                switch (v.ToLowerInvariant())
                {
                    case "magenta":
                        brush = 0xFFFF0097.ToSolidColorBrush();
                        break;

                    case "purple":
                        brush = 0xFFA200FF.ToSolidColorBrush();
                        break;

                    case "teal":
                        brush = 0xFF00ABA9.ToSolidColorBrush();
                        break;

                    case "lime":
                        brush = 0xFF8CBF26.ToSolidColorBrush();
                        break;

                    case "brown":
                        brush = 0xFFA05000.ToSolidColorBrush();
                        break;

                    case "pink":
                        brush = 0xFFE671B8.ToSolidColorBrush();
                        break;

                    case "orange":
                        brush = 0xFFF09609.ToSolidColorBrush();
                        break;

                    case "blue":
                        brush = 0xFF1BA1E2.ToSolidColorBrush();
                        break;

                    case "red":
                        brush = 0xFFE51400.ToSolidColorBrush();
                        break;

                    case "green":
                        brush = 0xFF339933.ToSolidColorBrush();
                        break;

                    case "mango":
                        brush = 0xFFF09609.ToSolidColorBrush();
                        break;
                }
            }

            return brush;
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A converter that takes a name of an accent color and returns a the corresponding localized name.
    /// </summary>
    public class AccentColorNameToLocalizedColorName : IValueConverter
    {

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "By design")]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            string v = value as string;
            if (null == v)
            {
                throw new ArgumentNullException("value");
            }

            if (v == "accent color")
                v = "Label";           

            ResourceManager rm = new global::System.Resources.ResourceManager("weekc.Languages.Strings", typeof(Strings).Assembly);
            string lv = rm.GetString("AccentColor" + v.First().ToString().ToUpper() + String.Join("", v.Skip(1)), CultureInfo.CurrentCulture);

            return lv;
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}