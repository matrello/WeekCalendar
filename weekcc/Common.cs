using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Media;
using Microsoft.Phone.UserData;
using System.Threading;
using System.Windows;

namespace weekcc
{
    // FM.2016.12.12 - released to the public. https://github.com/matrello/WeekCalendar

    [DataContract]
    public class AccountSetting
    {
        private bool enabled;
        private string accountColor;
        private string name;
        private string kind;

        public event EventHandler EnableChanged;

        protected virtual void OnEnableChanged(EventArgs e)
        {
            EventHandler handler = EnableChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public AccountSetting(bool enabled, string kind, string name, string accountColor)
        {
            this.enabled = enabled;
            this.kind = kind;
            this.name = name;
            this.accountColor = accountColor;
        }

        public void Copy(AccountSetting source)
        {
            this.enabled = source.Enabled;
            this.kind = source.Kind;
            this.name = source.Name;
            this.accountColor = source.AccountColor;
        }

        public string UniqueKey
        {
            get
            {
                return "Account$" + kind + "$" + name;
            }
        }
        
        [DataMember]
        public bool Enabled {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                OnEnableChanged(EventArgs.Empty);
            }
        }

        [DataMember]
        public string AccountColor
        {
            get
            {
                return accountColor;
            }
            set
            {
                accountColor = value;
                OnEnableChanged(EventArgs.Empty);
            }
        }

        public ReadOnlyCollection<string> Colors
        {
            get
            {
                return ColorExtensions.AccentColors();
            }
        }

        [DataMember]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        [DataMember]
        public string Kind
        {
            get
            {
                return kind;
            }
            set
            {
                kind = value;
            }
        }

        public string KindToShow
        {
            get
            {
                if ((kind == "WindowsLive") && (name == "Windows Live"))
                    return "";

                return kind;
            }
        }
    }

    static public class AppSettingsChanged
    {
        static public bool Changed = false;
    }

    public class AppSettings
    {
        private static Mutex mutex = new Mutex(false, "weekc_mutex");
        private IsolatedStorageSettings isolatedStore;

        const string ShowWeekendKeyName = "ShowWeekendSetting";
        const string ShowWeekNumberKeyName = "ShowWeekNumberSetting";
        const string DayBeginsKeyName = "DayBeginsSetting";
        const string DayEndsKeyName = "DayEndsSetting";
        const string AccountsKeyName = "AccountsSetting";
        const string ShowPrivateKeyName = "ShowPrivateSetting";
        const string ZoomOnStartKeyName = "ZoomOnStartSetting";

        const bool ShowWeekendSettingDefault = false;
        const bool ShowWeekNumberSettingDefault = true;
        const int DayBeginsSettingDefault = 9;
        const int DayEndsSettingDefault = 18;
        const bool ShowPrivateSettingDefault = true;
        const bool ZoomOnStartSettingDefault = false;

        private List<AccountSetting> accounts = new List<AccountSetting>();

        public void SetDayBeginsEndsDefaults()
        {
            DayBeginsSetting = DayBeginsSettingDefault;
            DayEndsSetting = DayEndsSettingDefault;
        }

        public AppSettings()
        {
            mutex.WaitOne();

            try
            {
                isolatedStore = IsolatedStorageSettings.ApplicationSettings;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using IsolatedStorageSettings: " + e.ToString());
            }

            if (!DesignerProperties.IsInDesignTool)
            {
                var phoneAccounts = new Microsoft.Phone.UserData.Appointments().Accounts;
                foreach (var phoneAccount in phoneAccounts)
                {
                    AccountSetting account = new AccountSetting(true, phoneAccount.Kind.ToString(), phoneAccount.Name.ToString(), "accent color");
                    account.EnableChanged += new EventHandler(Account_EnableChanged);
                    accounts.Add(account);
                }
            }

            mutex.ReleaseMutex();
        }

        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            if (isolatedStore.Contains(Key))
            {
                if (isolatedStore[Key] != value)
                {
                    isolatedStore[Key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                isolatedStore.Add(Key, value);
                valueChanged = true;
            }

            if (valueChanged)
                AppSettingsChanged.Changed = true;

            return valueChanged;
        }

        public valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
        {
            valueType value;

            if (isolatedStore.Contains(Key))
            {
                value = (valueType)isolatedStore[Key];
            }
            else
            {
                value = defaultValue;
            }

            return value;
        }

        public void Save()
        {
            mutex.WaitOne();
            isolatedStore.Save();
            mutex.ReleaseMutex();
        }

        public bool ShowWeekendSetting
        {
            get
            {
                return GetValueOrDefault<bool>(ShowWeekendKeyName, ShowWeekendSettingDefault);
            }
            set
            {
                AddOrUpdateValue(ShowWeekendKeyName, value);
                Save();
            }
        }

        public bool ShowWeekNumberSetting
        {
            get
            {
                return GetValueOrDefault<bool>(ShowWeekNumberKeyName, ShowWeekNumberSettingDefault);
            }
            set
            {
                AddOrUpdateValue(ShowWeekNumberKeyName, value);
                Save();
            }
        }

        public bool ShowPrivateSetting
        {
            get
            {
                return GetValueOrDefault<bool>(ShowPrivateKeyName, ShowPrivateSettingDefault);
            }
            set
            {
                AddOrUpdateValue(ShowPrivateKeyName, value);
                Save();
            }
        }

        public bool ZoomOnStartSetting
        {
            get
            {
                return GetValueOrDefault<bool>(ZoomOnStartKeyName, ZoomOnStartSettingDefault);
            }
            set
            {
                AddOrUpdateValue(ZoomOnStartKeyName, value);
                Save();
            }
        }

        public int DayBeginsSetting
        {
            get
            {
                return GetValueOrDefault<int>(DayBeginsKeyName, DayBeginsSettingDefault);
            }
            set
            {
                AddOrUpdateValue(DayBeginsKeyName, value);
                Save();
            }
        }

        public int DayEndsSetting
        {
            get
            {

                return GetValueOrDefault<int>(DayEndsKeyName, DayEndsSettingDefault);
            }
            set
            {
                AddOrUpdateValue(DayEndsKeyName, value);
                Save();
            }
        }

        public DateTime DayBeginsSettingAsDate
        {
            get
            {
                return new DateTime(1972, 3, 11, GetValueOrDefault<int>(DayBeginsKeyName, DayBeginsSettingDefault), 0, 0);
            }
            set
            {
                AddOrUpdateValue(DayBeginsKeyName, value.Hour);
                Save();
            }
        }

        public DateTime DayEndsSettingAsDate
        {
            get
            {
                return new DateTime(1972, 3, 11, GetValueOrDefault<int>(DayEndsKeyName, DayEndsSettingDefault), 0, 0);
            }
            set
            {
                AddOrUpdateValue(DayEndsKeyName, value.Hour);
                Save();
            }
        }

        public List<AccountSetting> AccountsSetting
        {
            get
            {
                foreach (AccountSetting account in accounts)
                {
                    if (isolatedStore.Contains(account.UniqueKey))
                        account.Copy((AccountSetting)isolatedStore[account.UniqueKey]);
                }
                

                return accounts;
            }

        }

        void Account_EnableChanged(object sender, EventArgs e)
        {
            AccountSetting account = (AccountSetting)sender;
            AddOrUpdateValue(account.UniqueKey, account);
            Save();
        }

        protected string GetHourString(int hour)
        {
            string designator = "";
            int hourTrim = 0;

            if (DateTimeFormatInfo.CurrentInfo.LongTimePattern.Contains("H"))
                return hour.ToString() + ":00";
            else
            {
                designator = (hour >= 12 ? "PM" : "AM");
                hourTrim = (hour > 12 ? 12 : 0);

                return (hour - hourTrim) + " " + designator;
            }
        }
    }

    /// <summary>
    /// Simple Logger class for WP7
    /// Written by Francesco "Matro" Martire, v2011.07.001
    /// </summary>
    public class Logger
    {
        private string name;
        private bool enabled;

        public bool Enabled
        {
            get { return enabled; }

            set
            {
                enabled = value;
                if (value)
                {
                    var asm = Assembly.GetExecutingAssembly();
                    var parts = asm.FullName.Split(',');
                    string version = parts[1].Split('=')[1];

                    AppendCR();
                    AppendContent("I", string.Format("{0} {1} Logger enabled", name, version));
                }
            }
        }

        public Logger(string Name)
        {
            name = Name;

            if (System.Diagnostics.Debugger.IsAttached)
                Enabled = true;
        }

        public void AppendError(string content, params object[] args)
        {
            AppendContent("E!", content, args);
        }

        public void AppendInfo(string content, params object[] args)
        {
            AppendContent("I ", content, args);
        }

        public void AppendWarn(string content, params object[] args)
        {
            AppendContent("W!", content, args);
        }

        private void AppendContent(string level, string content, params object[] args)
        {
            Debug.WriteLine(string.Format("{0} {1} - {2}", DateTime.Now.ToString("o"), level, string.Format(content, args)));
        }

        public void AppendCR()
        {
            Debug.WriteLine("");
        }
    }

    /// <summary>
    /// Adds extension methods relating to color.
    /// PhoneToolkitSample.Data November 2011
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// An array of all the names of the accent colors.
        /// </summary>
        private static string[] _accentColors = { "accent color",
                                                  "magenta", 
                                                  "purple",
                                                  "teal", 
                                                  "lime", 
                                                  "brown", 
                                                  "pink", 
                                                  "mango",
                                                  "blue",
                                                  "red",
                                                  "green" };


        /// <summary>
        /// Returns an array of all the names of the accent colors.
        /// </summary>
        public static ReadOnlyCollection<string> AccentColors()
        {
            return new ReadOnlyCollection<string>(_accentColors);
        }

        /// <summary>
        /// Returns a Color for a hex value.
        /// </summary>
        /// <param name="argb">The hex value</param>
        /// <remarks>Calls to this method should look like 0xFF11FF11.ToColor().</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "argb", Justification = "By design")]
        public static Color ToColor(this uint argb)
        {
            return Color.FromArgb((byte)((argb & 0xff000000) >> 0x18),
                                  (byte)((argb & 0xff0000) >> 0x10),
                                  (byte)((argb & 0xff00) >> 8),
                                  (byte)(argb & 0xff));
        }

        /// <summary>
        /// Returns a SolidColorBrush for a hex value.
        /// </summary>
        /// <param name="argb">The hex value</param>
        /// <remarks>Calls to this method should look like 0xFF11FF11.ToSolidColorBrush().</remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "argb", Justification = "By design")]
        public static SolidColorBrush ToSolidColorBrush(this uint argb)
        {
            return new SolidColorBrush(argb.ToColor());
        }

        // FM 2013.08 simulates opacity to xaml defined color
        public static Brush GetBlendedBrush(SolidColorBrush brush, double opacity)
        {
            Color argb = brush.Color;
            Color blend = (Color)Application.Current.Resources["PhoneBackgroundColor"];
            argb.A = Convert.ToByte(opacity * 255.0);

            Color color = Color.FromArgb(0xFF,
                Convert.ToByte((argb.A / 255.0) * argb.R + (1.0 - argb.A / 255.0) * blend.R),
                Convert.ToByte((argb.A / 255.0) * argb.G + (1.0 - argb.A / 255.0) * blend.G),
                Convert.ToByte((argb.A / 255.0) * argb.B + (1.0 - argb.A / 255.0) * blend.B));

            return new SolidColorBrush(color);
        }
    }

#if FAKE_APPOINTMENT

    // this is used to simulate the presence of Appointments on WP7 emulator
    public class Appointment
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public string Subject { get; set; }
        public string Details { get; set; }
        public bool IsAllDayEvent;
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

}

