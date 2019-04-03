using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FTPviaMFP
{
    public sealed partial class ConnectDialog : ContentDialog
    {
        ApplicationDataContainer localSettings;

        public ConnectDialog()
        {
            this.InitializeComponent();
            localSettings = ApplicationData.Current.LocalSettings;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Application.Current.Exit();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            localSettings.Values[App.IPADDRESS] = ipBox.Text;
            localSettings.Values[App.PASSWORD] = paswdBox.Password;
        }
    }
}
