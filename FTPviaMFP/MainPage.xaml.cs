using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FTPviaMFP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        string strServerName = string.Empty;
        string strAdminPaswd = string.Empty;

        ApplicationDataContainer localSettings;
        private BoxSession boxSession = null;

        public MainPage()
        {
            this.InitializeComponent();
            localSettings = ApplicationData.Current.LocalSettings;
            boxSession = new BoxSession();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                do {
                } while (false == await InputValidation());

                ContentDialog content = new ContentDialog();
                content.Title = Helper.GetResourceString("ID_LOGIN_TITLE");
                content.Content = Helper.GetResourceString("ID_CONNECTING");
                content.ShowAsync();

                bool bRet = await LaunchApplication();
                content.Hide();

                if (!bRet)
                {
                    await Task.Delay(3000);
                    boxes.Text = string.Empty;
                    this.OnNavigatedTo(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage :: OnNavigatedTo() :: Exception Handled: " + ex);
            }
        }

        async Task<bool> InputValidation()
        {
            bool bValidIP = false;
            try
            {
                ConnectDialog connectDialog = new ConnectDialog();
                await connectDialog.ShowAsync();

                strServerName = localSettings.Values[App.IPADDRESS].ToString();
                strAdminPaswd = localSettings.Values[App.PASSWORD].ToString();

                if ( string.IsNullOrEmpty(strServerName) || string.IsNullOrEmpty(strAdminPaswd) )
                {
                    ContentDialog dialog = Helper.GetDialog();
                    dialog.Content = Helper.GetResourceString("ID_CRED_EMPTY");
                    await dialog.ShowAsync();
                }
                else
                {
                    IPAddress iPAddress;
                    if (IPAddress.TryParse(strServerName, out iPAddress))
                        bValidIP = true;
                    else
                    {
                        ContentDialog dialog = Helper.GetDialog();
                        dialog.Content = Helper.GetResourceString("ID_INCORRECT_IP");
                        await dialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage :: InputValidation() :: Exception Handled: " + ex);
            }
            return bValidIP;
        }

        async Task<bool> LaunchApplication()
        {
            bool bRet = false;
            try
            {
                boxSession.MFPSession.ServerName = strServerName;
                boxSession.MFPSession.IsValidSession = false;

                boxSession.MFPName = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_MFP_NAME);
                if (!string.IsNullOrEmpty(boxSession.MFPName))
                {
                    bRet = await BoxInterface.BoxOpenConnection(boxSession);

                    if (boxSession.MFPSession.AdminAccessMode == EAccess.EBX)
                    {
                        bool bReturn = await BoxInterface.BoxGetSecuritySettings(boxSession);
                        if (bReturn)
                        {
                            boxSession.AuthSettings.HTTPPassword = strAdminPaswd;
                            bReturn = await BoxInterface.BoxAuthenticate(boxSession);
                        }
                        else
                            boxes.Text = Helper.GetResourceString("ID_FAIL_SEC_SETTINGS");
                        bRet = bReturn;
                    }

                    if (bRet)
                    {
                        await BoxInterface.BoxGetEngineInfo(boxSession, strAdminPaswd);
                        bRet = await BoxInterface.BoxGetBoxList(boxSession);

                        if (boxSession.Boxes != null)
                        {
                            //foreach (EBBox eBBox in boxSession.Boxes)
                            //{
                            //    boxes.Text += eBBox.BoxID.ToString("D3") + " " + eBBox.BoxName + "\r\n";
                            //}
                            //boxes.Text = boxes.Text.TrimEnd(new char[] { '\r', '\n' });
                            bRet = true;
                            this.Frame.Navigate(typeof(AppPage), boxSession);
                        }
                        else
                            boxes.Text = Helper.GetResourceString("ID_FAIL_BOX_POPULATE");
                    }
                    else
                        boxes.Text = Helper.GetResourceString("ID_FAIL_CONN_AUTH");
                }
                else
                    boxes.Text = Helper.GetResourceString("ID_FAIL_NO_DEVICE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MainPage :: LaunchApplication() :: Exception Handled: " + ex);
            }
            return bRet;
        }
    }
}
