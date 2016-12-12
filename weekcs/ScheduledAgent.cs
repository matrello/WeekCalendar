using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using weekcc;
using weekcs.Languages;

namespace weekcs
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            Logger logger = new Logger("Week Calendar Scheduled Task");

#if DEBUG_TOASTS
            ShellToast toast = new ShellToast();
            toast.Title = "PeriodicTask test";
            toast.Content = "004 OK";
            toast.Show();
#endif

//#if DEBUG_AGENT
//            WeekTileController weekTileController = new WeekTileController(logger, DateTime.Now.ToString(), DateTime.Now.ToString());
//#else

    #if DEBUG_TOASTS
            WeekTileController weekTileController = new WeekTileController(logger, DateTime.Now.ToString());
    #else
            WeekTileController weekTileController = new WeekTileController(logger, Strings.Title, Strings.WeekShortLabel);
    #endif
//#endif
            weekTileController.TileRefresh(true);

#if DEBUG_AGENT
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(30));
#endif

#if DEBUG_TOASTS
            toast = new ShellToast();
            toast.Title = "PeriodicTask test";
            toast.Content = "005 OK";
            toast.Show();
#endif

            NotifyComplete();
        }
    }

}