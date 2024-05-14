using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VideoOS.Platform.SDK.UI.LoginDialog;

namespace ConfigAccessViaSDK
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Guid integrationId = new Guid("6F31CE44-3E9D-44A5-95CF-2E17FABC494D");
        public static string integrationName = "Config Access via SDK";
        public static string manufacturerName = "Sample Manufacturer";
        public static string version = "2.0";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            VideoOS.Platform.SDK.Environment.Initialize();          
            VideoOS.Platform.SDK.UI.Environment.Initialize();       

            bool connected = false;
            DialogLoginForm loginForm = new DialogLoginForm(
                new DialogLoginForm.SetLoginResultDelegate((b) => connected = b),
                integrationId,
                integrationName,
                version,
                manufacturerName);
            loginForm.ShowDialog();								

            if (!connected)
            {
                Current.Shutdown();
            }
        }
    }
}