using Datasheets2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Datasheets2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        const int MAX_STACKTRACE = 2;
        static SemaphoreSlim dialogLock = new SemaphoreSlim(1);

        public static new App Current { get { return (App)Application.Current; } }

        Database db;
        public Database Database { get { return db; } }

        // TODO: Load from settings
        // Just use current working directory for now.
        public string DocumentsDir { get { return System.IO.Directory.GetCurrentDirectory(); } }

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            db = new Database();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorHandler(e.Exception);
        }

        public static void ErrorHandler(string message, string title = "Error", bool fatal = true)
        {
            // Prevent showing more than one dialog
            if (dialogLock.Wait(0))
            {
                try
                {
                    MessageBox.Show(message, caption: title, button: MessageBoxButton.OK, icon: MessageBoxImage.Error);

                    if (fatal)
                        Application.Current.Shutdown();
                }
                finally
                {
                   // dialogLock.Release();
                }
            }
        }

        public static void ErrorHandler(Exception ex, bool fatal = true)
        {
            string exType = ex.GetType().Name;

            StringBuilder message = new StringBuilder();
            message.AppendLine(exType + ":");
            message.AppendLine(ex.Message);

            // Append stack trace
            if (ex.StackTrace != null)
            {
                message.AppendLine();

                // Limit stacktrace lines
                var stackTrace = ex.StackTrace.Split('\n').ToList();
                if (stackTrace.Count() > MAX_STACKTRACE)
                {
                    stackTrace = stackTrace.Take(MAX_STACKTRACE).ToList();
                }
                message.AppendLine(String.Join("\n", stackTrace));
            }
           
            ErrorHandler(message.ToString(), title: exType, fatal: fatal);
        }
    }
}
