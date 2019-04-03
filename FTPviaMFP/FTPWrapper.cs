using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentFTP;

namespace FTPviaMFP
{
    class FTPWrapper
    {
        private static FtpClient ftpClient = null;
        static List<string> allItems = new List<string>();

        public async static Task OpenConnection(EBMFPSession eBMFPSession, string strFTPUsername, string strPassword)
        {
            try
            {
                ftpClient = new FtpClient(eBMFPSession.ServerName, strFTPUsername, strPassword);
                await ftpClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FTPWrapper :: OpenConnection() :: Exception Handled: " + ex);
            }
        }

        public async static Task<bool> Download(BoxSession boxSession, string strLocalTarget, string strRemoteBoxPath, EBProgressInfo eBProgressInfo)
        {
            bool bSuccess = false;
            try
            {
                allItems.Clear();
                await GetTotalFileListing(strRemoteBoxPath);
                eBProgressInfo.TotalDocuments = (ulong)allItems.Count;
                eBProgressInfo.DocumentsTransfered = 0;

                foreach (string strRemoteFile in allItems)
                {
                    eBProgressInfo.FolderName = "//  " + strRemoteFile.Split('/').GetValue(1).ToString();
                    ++eBProgressInfo.DocumentsTransfered;
                    boxSession.EUIUpdtEvt(eBProgressInfo, EventArgs.Empty);

                    string strFilePath = strLocalTarget + "\\" + strRemoteFile;
                    bSuccess = await Task.Run(async () =>
                    {
                        return await ftpClient.DownloadFileAsync(strFilePath, strRemoteFile, true, FtpVerify.None, null);
                    });

                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FTPWrapper :: Download() :: Exception Handled: " + ex);
            }
            return bSuccess;
        }

        private async static Task GetTotalFileListing(string strRemotePath)
        {
            try
            {
                FtpListItem[] temp = await ftpClient.GetListingAsync(strRemotePath);
                foreach (FtpListItem item in temp)
                {
                    if (item.Type == FtpFileSystemObjectType.File)
                        allItems.Add(GetRelativePath(item.FullName));
                    else if (item.Type == FtpFileSystemObjectType.Directory)
                    {
                        strRemotePath = item.FullName;
                        await GetTotalFileListing(strRemotePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FTPWrapper :: GetTotalFileListing() :: Exception Handled: " + ex);
            }
        }

        private static string GetRelativePath(string strPath)
        {
            string strRelPath = string.Empty;
            try
            {
                char[] strSeparators = new char[] { '/' };
                string[] stPaths = strPath.Split(strSeparators, StringSplitOptions.RemoveEmptyEntries);

                for (int index = 3; index < stPaths.Length; index++)
                    strRelPath += stPaths[index] + "/";

                strRelPath = strRelPath.TrimEnd(strSeparators);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FTPWrapper :: GetRelativePath() :: Exception Handled: " + ex);
            }
            return strRelPath;
        }
    }
}
