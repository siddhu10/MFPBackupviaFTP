using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace FTPviaMFP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AppPage : Page
    {
        public ObservableCollection<EBBox> MFPBoxes { get; set; }
        BoxSession boxSession = null;

        public AppPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                if (e.Parameter != null)
                {
                    boxSession = e.Parameter as BoxSession;

                    if (boxSession.Boxes != null)
                    {
                        MFPBoxes = new ObservableCollection<EBBox>();
                        foreach (EBBox boxObj in boxSession.Boxes)
                        {
                            if (boxObj.IsProtected)
                                boxObj.BoxIcon = new BitmapImage(new Uri("ms-appx:///Assets/LCube.png"));
                            else
                                boxObj.BoxIcon = new BitmapImage(new Uri("ms-appx:///Assets/Cube.png"));
                            MFPBoxes.Add(boxObj);
                        }
                    }
                    
                    mfpLabel.Text = boxSession.MFPName;
                    ipLabel.Text = boxSession.MFPSession.ServerName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: OnNavigatedTo() :: Exception Handled: " + ex);
            }
        }

        private void BkpBtn_Click(object sender, RoutedEventArgs e)
        {
            StartBackup();
        }

        async void StartBackup()
        {
            int index = 0;
            int iSelBoxCount = 0;
            EBBox[] selBoxList;
            EventHandler updateEvent;

            try
            {
                brwsBtn.IsEnabled = false;
                bkpBtn.IsEnabled = false;
                logoutButton.IsEnabled = false;

                iSelBoxCount = boxView.SelectedItems.Count;
                ContentDialog dialog = Helper.GetDialog();

                if (iSelBoxCount > 0)
                {
                    selBoxList = new EBBox[iSelBoxCount];
                    foreach (object obj in boxView.SelectedItems)
                    {
                        EBBox ebObj = obj as EBBox;
                        selBoxList[index++] = ebObj;
                    }

                    ApplicationDataContainer applicationData = ApplicationData.Current.LocalSettings;
                    string strPaswd = applicationData.Values[App.PASSWORD].ToString();

                    string strFTPAccount = await BoxInterface.CreateFTPAccount(boxSession, strPaswd);

                    updateEvent = new EventHandler(UpdateUI);
                    boxSession.EUIUpdtEvt = updateEvent;

                    progDlg.Visibility = Visibility.Visible;
                    string strMsg = await BoxInterface.BoxBackup(boxSession, strPaswd, strFTPAccount, true, iSelBoxCount, selBoxList);
                    progDlg.Visibility = Visibility.Collapsed;

                    dialog.Content = strMsg;
                    await dialog.ShowAsync();

                    await BoxInterface.BoxGetBoxList(boxSession);
                    this.Frame.Navigate(typeof(AppPage), boxSession);
                }
                else
                {
                    dialog.Content = Helper.GetResourceString("ID_SELECT_BOX");
                    await dialog.ShowAsync();
                }

                logoutButton.IsEnabled = true;
                bkpBtn.IsEnabled = true;
                brwsBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: StartBackup() :: Exception Handled: " + ex);
            }
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            try
            {
                if (progDlg.Visibility == Visibility.Collapsed)
                {
                    statusMsg.Foreground = new SolidColorBrush(Colors.White);

                    if (sender is EBProgressInfo)
                    {
                        EBProgressInfo eBProgressInfo = sender as EBProgressInfo;

                        if (eBProgressInfo.OperationInfo == EOperation.ABORTING ||
                            eBProgressInfo.OperationInfo == EOperation.SKIPPING)
                            statusMsg.Foreground = new SolidColorBrush(Colors.Red);
                        else
                            statusMsg.Foreground = (SolidColorBrush)Application.Current.Resources["sysColor"];

                        string stTemp = string.IsNullOrEmpty(eBProgressInfo.BoxName) ? string.Empty : eBProgressInfo.BoxName;
                        string strFoldrName = string.IsNullOrEmpty(eBProgressInfo.FolderName) ? string.Empty : eBProgressInfo.FolderName;

                        statusMsg.Text = Helper.GetOperationString(eBProgressInfo.OperationInfo) + "  " + stTemp + "\r\n" + strFoldrName;
                    }
                    else if (sender is string)
                    {
                        string stMsg = sender as string;
                        statusMsg.Text = stMsg;
                    }
                    else
                        statusMsg.Text = string.Empty;
                }
                else
                {
                    if (sender is EBProgressInfo)
                    {
                        EBProgressInfo eBProgressInfo = sender as EBProgressInfo;

                        opText.Text = Helper.GetOperationString(eBProgressInfo.OperationInfo) + " ...";
                        boxText.Text = string.IsNullOrEmpty(eBProgressInfo.BoxName) ? string.Empty : eBProgressInfo.BoxName;
                        docText.Text = string.IsNullOrEmpty(eBProgressInfo.FolderName) ? string.Empty : eBProgressInfo.FolderName;

                        if (eBProgressInfo.OperationInfo == EOperation.DOWNLOADING)
                        {
                            progressBar.IsIndeterminate = false;

                            progressBar.Value = (eBProgressInfo.TotalDocuments == 0) ? 0 : (eBProgressInfo.DocumentsTransfered * 100) / eBProgressInfo.TotalDocuments;
                            progressText.Text = progressBar.Value + " %";
                        }
                        else
                        {
                            progressBar.IsIndeterminate = true;
                            progressText.Text = string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: UpdateUI() :: Exception Handled: " + ex);
            }
        }

        private async void BrwsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileSavePicker fileOpenPicker = new FileSavePicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                fileOpenPicker.FileTypeChoices.Add("Zip Content", new List<string>() { ".zip" });

                DateTime date = DateTime.Now;
                string strDate = date.Year + date.Month.ToString("D2") + date.Day.ToString("D2") + "_" + date.Hour.ToString("D2") + date.Minute.ToString("D2") + date.Second.ToString("D2");
                fileOpenPicker.SuggestedFileName = strDate;
                boxSession.TargetFile = await fileOpenPicker.PickSaveFileAsync();

                boxSession.TargetFolder = await GetFolderPath(boxSession.TargetFile);
                if (boxSession.TargetFile != null)
                {
                    trgtPath.Text = boxSession.TargetFile.Path;
                    bkpBtn.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: BrwsBtn_Click() :: Exception Handled: " + ex);
            }
        }

        private async Task<StorageFolder> GetFolderPath(StorageFile pickedFile)
        {
            StorageFolder storageFolder = null;
            try
            {
                string folder = pickedFile.Path.Substring(0, pickedFile.Path.LastIndexOf("\\"));
                storageFolder = await StorageFolder.GetFolderFromPathAsync(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: GetFolderPath() :: Exception Handled: " + ex);
            }
            return storageFolder;
        }

        private async void OnLogoutButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationDataContainer applicationData = ApplicationData.Current.LocalSettings;
                string strPaswd = applicationData.Values[App.PASSWORD].ToString();

                await BoxInterface.BoxDeleteFTPAccount(boxSession, strPaswd);
                await BoxInterface.BoxCloseConnection(boxSession);

                this.Frame.Navigate(typeof(MainPage));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AppPage :: OnLogoutButtonClick() :: Exception Handled: " + ex);
            }
        }

        private void HideBtn_Click(object sender, RoutedEventArgs e)
        {
            progDlg.Visibility = Visibility.Collapsed;
            statusMsg.Text = string.Empty;
            progBar.Visibility = Visibility.Visible;
        }
    }
}
