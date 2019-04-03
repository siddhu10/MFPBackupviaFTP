using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.Storage;
using Windows.Web.Http;

namespace FTPviaMFP
{
    class BoxInterface
    {
        static async Task<HttpResponseMessage> SendAndGetHTTPCmdResp(EHTTPReqType eHTTPReqType, EBMFPSession eBMFPSession, string strURL, string strParams)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage();
            try
            {
                if (eHTTPReqType == EHTTPReqType.GET)
                    responseMessage = await HTTPWrapper.GetRequest(eBMFPSession, strURL, strParams);
                else
                    responseMessage = await HTTPWrapper.PostRequest(eBMFPSession, strURL, strParams);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: SendAndGetHTTPCmdResp() :: Exception Handled: " + ex);
            }
            return responseMessage;
        }

        static async Task<EBoxError> IsCommandSuccess(HttpResponseMessage responseMessage)
        {
            EBoxError eBoxError = EBoxError.FAIL;
            try
            {
                if (responseMessage != null && responseMessage.StatusCode == Windows.Web.Http.HttpStatusCode.Ok)
                {
                    string strResult = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_RESULT);

                    if (!string.IsNullOrEmpty(strResult))
                    {
                        eBoxError = (EBoxError)Convert.ToInt32(strResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: IsCommandSuccess() :: Exception Handled: " + ex);
            }
            return eBoxError;
        }

        public static async Task<bool> BoxOpenConnection(BoxSession boxSession)
        {
            bool bSuccess = false;
            EBoxError eBoxError;
            try
            {
                boxSession.MFPSession.ServerPort = 8080;
                boxSession.MFPSession.HTTPTimeOut = 10000;
                boxSession.MFPSession.ScopeId = 0;
                boxSession.MFPSession.IsValidSession = true;

                IPAddress iPAddress = IPAddress.Parse(boxSession.MFPSession.ServerName);
                if (iPAddress.AddressFamily.ToString() == AddressFamily.InterNetworkV6.ToString())
                    boxSession.MFPSession.ScopeId = Convert.ToUInt64(iPAddress.ScopeId);

                await GetHTTPInfo(boxSession);
                await GetFTPInfo(boxSession);

                HttpResponseMessage responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWCONNECT, string.Empty);

                boxSession.EngineInfo.EngineVersion = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_ENGINEVERSION);

                if (EBoxError.S_BOXNW_OK == (eBoxError = await IsCommandSuccess(responseMessage)))
                {
                    string strBoxSession = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_SESSIONID);
                    if (!string.IsNullOrEmpty(strBoxSession))
                        boxSession.MFPSession.BoxSession = Convert.ToUInt64(strBoxSession);

                    string strAccessMode = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_ACCESSMODE);
                    if (!string.IsNullOrEmpty(strAccessMode))
                        boxSession.MFPSession.AdminAccessMode = (EAccess)Convert.ToInt32(strAccessMode);

                    boxSession.MFPSession.IsValidSession = true;
                    bSuccess = true;
                }
                else
                    if (eBoxError == EBoxError.S_BOXNW_SYS_FUNCTION_ERROR)
                        boxSession.MFPSession.AdminAccessMode = EAccess.EBX;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxOpenConnection() :: Exception Handled: " + ex);
            }
            return bSuccess;
        }

        static async Task GetHTTPInfo(BoxSession boxSession)
        {
            try
            {
                string strPort = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_HTTP_PORTNO);
                if (string.IsNullOrEmpty(strPort))
                {
                    boxSession.MFPSession.HTTPServerPort = 8080;
                    boxSession.MFPSession.HTTPSSLEnabled = EState.DISABLED;
                }
                else
                {
                    boxSession.MFPSession.HTTPServerPort = Convert.ToInt32(strPort);

                    string strHTTPEnabled = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_HTTP_SSLPORTSTATE);
                    if (string.IsNullOrEmpty(strHTTPEnabled))
                        boxSession.MFPSession.HTTPSSLEnabled = EState.DISABLED;
                    else
                    {
                        EState state = (EState)Convert.ToInt32(strHTTPEnabled);

                        if (state == EState.DISABLED)
                            boxSession.MFPSession.HTTPSSLEnabled = state;
                        else
                        {
                            string strSSLPortNo = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_HTTP_SSLPORTNO);
                            if (string.IsNullOrEmpty(strSSLPortNo))
                                boxSession.MFPSession.HTTPSSLEnabled = EState.DISABLED;
                            else
                            {
                                boxSession.MFPSession.HTTPSSLEnabled = EState.ENABLED;
                                boxSession.MFPSession.HTTPServerPort = Convert.ToInt32(strSSLPortNo);
                            }
                        }
                    }
                }
                boxSession.MFPSession.ServerPort = boxSession.MFPSession.HTTPServerPort;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: GetHTTPInfo() :: Exception Handled: " + ex);
            }
        }

        static async Task GetFTPInfo(BoxSession boxSession)
        {
            try
            {
                string strPort = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_FTP_PORTNO);
                if (string.IsNullOrEmpty(strPort))
                {
                    boxSession.MFPSession.FTPServerPort = 21;
                    boxSession.MFPSession.FTPSSLEnabled = EState.DISABLED;
                }
                else
                {
                    boxSession.MFPSession.FTPServerPort = Convert.ToInt32(strPort);

                    string strFTPEnabled = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_FTP_SSLPORTSTATE);
                    if (string.IsNullOrEmpty(strFTPEnabled))
                        boxSession.MFPSession.FTPSSLEnabled = EState.DISABLED;
                    else
                    {
                        EState state = (EState)Convert.ToInt32(strFTPEnabled);

                        if (state == EState.DISABLED)
                            boxSession.MFPSession.FTPSSLEnabled = state;
                        else
                        {
                            string strSSLPortNo = await SNMPWrapper.GetOIDValue(boxSession.MFPSession.ServerName, SNMPWrapper.OID_FTP_SSLPORTNO);
                            if (string.IsNullOrEmpty(strSSLPortNo))
                                boxSession.MFPSession.FTPSSLEnabled = EState.DISABLED;
                            else
                            {
                                boxSession.MFPSession.FTPSSLEnabled = EState.ENABLED;
                                boxSession.MFPSession.FTPServerPort = Convert.ToInt32(strSSLPortNo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: GetFTPInfo() :: Exception Handled: " + ex);
            }
        }

        public async static Task<bool> BoxGetSecuritySettings(BoxSession boxSession)
        {
            bool bSuccess = false;
            try
            {
                HttpResponseMessage responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWGETSECURITYSETTING, string.Empty);
                if (EBoxError.S_BOXNW_OK == await IsCommandSuccess(responseMessage))
                {
                    boxSession.AuthSettings.HTTPUserName = "admin";
                    boxSession.AuthSettings.Encryption = false;

                    string strSecMode = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_SECURITYMODE);
                    if (!string.IsNullOrEmpty(strSecMode))
                        boxSession.AuthSettings.SecurityMode = (ESecurityMode)Convert.ToInt32(strSecMode);

                    string strAuthEnable = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_AUTHSTATE);
                    if (!string.IsNullOrEmpty(strAuthEnable))
                        boxSession.AuthSettings.AuthState = Convert.ToInt32(strAuthEnable) == 1 ? true : false;

                    string strAuthMthd = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_AUTHMODE);
                    if (!string.IsNullOrEmpty(strAuthMthd))
                        boxSession.AuthSettings.AuthMode = (EAuthMethod)Convert.ToInt32(strAuthMthd);

                    if (boxSession.AuthSettings.AuthState == false && boxSession.AuthSettings.AuthMode != EAuthMethod.DISABLE)
                        boxSession.AuthSettings.AuthMode = EAuthMethod.DISABLE;

                    string strHashAlgo = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_HASHALGO);
                    if (!string.IsNullOrEmpty(strHashAlgo))
                        boxSession.AuthSettings.HashAlgo = (EHashAlgo)Convert.ToInt32(strHashAlgo);

                    boxSession.AuthSettings.HTTPDomain = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_DOMAINS);
                    bSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxGetSecuritySettings() :: Exception Handled: " + ex);
            }
            return bSuccess;
        }

        public async static Task<bool> BoxAuthenticate(BoxSession boxSession)
        {
            bool bSuccess = false;
            try
            {
                if ((boxSession.AuthSettings.AuthMode < EAuthMethod.USRLOCAL) && (!string.IsNullOrEmpty(boxSession.AuthSettings.HTTPPassword)))
                {
                    int iEncEnable = boxSession.AuthSettings.Encryption ? 1 : 0;
                    string strParam = String.Format("?UID={0}&PWD={1}&APL={2}&ENC={3}", boxSession.AuthSettings.HTTPUserName, boxSession.AuthSettings.HTTPPassword, Helper.APPLN_NAME, iEncEnable);

                    HttpResponseMessage responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWAUTHENTICATE, strParam);
                    if (EBoxError.S_BOXNW_OK == await IsCommandSuccess(responseMessage))
                    {
                        string strAuthorized = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_AUTHORIZE);
                        if (!string.IsNullOrEmpty(strAuthorized))
                        {
                            bSuccess = Convert.ToInt32(strAuthorized) == 1 ? true : false;

                            if (bSuccess)
                            {
                                string strBoxSession = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_WebSessionID);
                                if (!string.IsNullOrEmpty(strBoxSession))
                                    boxSession.MFPSession.BoxSession = Convert.ToUInt64(strBoxSession);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxAuthenticate() :: Exception Handled: " + ex);
            }
            return bSuccess;
        }

        public async static Task<bool> BoxGetBoxList(BoxSession boxSession)
        {
            bool bSuccess = false;
            try
            {
                if (EAccess.RESTRICTED != boxSession.MFPSession.AdminAccessMode)
                {
                    string strParam = string.Empty;
                    HttpResponseMessage responseMessage;
                    EBoxError eError = EBoxError.FAIL;

                    if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                    {
                        strParam = String.Format("SID={0}", boxSession.MFPSession.BoxSession);
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWGETBOXLIST, strParam);
                        eError = await IsCommandSuccess(responseMessage);
                    }
                    else
                    {
                        strParam = String.Format("?SID={0}", boxSession.MFPSession.BoxSession);
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWGETBOXLIST, strParam);
                        eError = await IsCommandSuccess(responseMessage);
                    }

                    if (eError == EBoxError.S_BOXNW_OK)
                    {
                        string strBoxes = await Helper.GetPropValue(responseMessage, Helper.TAGNAME_BOXES, Helper.PROPNAME_NUMBOXES);
                        if (!string.IsNullOrEmpty(strBoxes))
                        {
                            boxSession.BoxCount = Convert.ToUInt64(strBoxes);
                            boxSession.Boxes = new List<EBBox>();

                            await Helper.PopulateBoxes(responseMessage, boxSession);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxGetBoxList() :: Exception Handled: " + ex);
            }
            return bSuccess;
        }

        public async static Task<string> CreateFTPAccount(BoxSession boxSession, string strPassword)
        {
            string strFTPAcntName = string.Empty;
            try
            {
                if (boxSession.MFPSession != null && boxSession.MFPSession.BoxSession != 0 && EAccess.RESTRICTED != boxSession.MFPSession.AdminAccessMode)
                {
                    string strParams = string.Empty;
                    HttpResponseMessage responseMessage;

                    if (EAccess.EBX == boxSession.MFPSession.AdminAccessMode)
                    {
                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}", boxSession.MFPSession.BoxSession);
                        else
                            strParams = String.Format("?SID={0}", boxSession.MFPSession.BoxSession);
                    }
                    else
                    { 
                        if (EAccess.BYPASS == boxSession.MFPSession.AdminAccessMode)
                            strPassword = BoxSession.DEFAULT_ADMIN_PASSWORD;

                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                        else
                            strParams = String.Format("?SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                    }

                    if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWCREATEFTPACCOUNT, strParams);
                    else
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWCREATEFTPACCOUNT, strParams);

                    if (EBoxError.S_BOXNW_OK == await IsCommandSuccess(responseMessage))
                    {
                        strFTPAcntName = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_LOGINNAME);

                        if (EAccess.EBX == boxSession.MFPSession.AdminAccessMode)
                            boxSession.MFPSession.HTTPPassword = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_LOGINPASSWORD);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: CreateFTPAccount() :: Exception Handled: " + ex);
            }
            return strFTPAcntName;
        }

        public async static Task<string> BoxBackup(BoxSession boxSession, string strAdminPaswd, string strFTPAcnt, bool bIsBackup, int iSelBoxCnt, EBBox[] selBoxes)
        {
            string strMsg = string.Empty;
            EBProgressInfo eBProgressInfo = new EBProgressInfo();
            try
            {
                eBProgressInfo.OperationInfo = EOperation.PROCESSING;
                boxSession.EUIUpdtEvt(eBProgressInfo, EventArgs.Empty);
                await BoxGetBoxProperty(boxSession, selBoxes, strAdminPaswd);

                await FTPWrapper.OpenConnection(boxSession.MFPSession, strFTPAcnt, boxSession.MFPSession.HTTPPassword);
                foreach (EBBox ebBox in selBoxes)
                {
                    if (ebBox.IsProtected)
                        eBProgressInfo.BoxName = Helper.GetResourceString("ID_PROTECTED_BOX") + ebBox.BoxName;
                    else
                        eBProgressInfo.BoxName = ebBox.BoxName;

                    eBProgressInfo.OperationInfo = EOperation.DOWNLOADING;
                    boxSession.EUIUpdtEvt(eBProgressInfo, EventArgs.Empty);

                    await BoxDownload(boxSession, ebBox, eBProgressInfo);

                    await Task.Delay(2000);
                    eBProgressInfo.OperationInfo = EOperation.ZIPPING;
                    eBProgressInfo.FolderName = string.Empty;
                    boxSession.EUIUpdtEvt(eBProgressInfo, EventArgs.Empty);

                    await BoxZip(boxSession, ebBox);
                    await Task.Delay(3000);
                }

                eBProgressInfo.OperationInfo = EOperation.ZIPPING;
                eBProgressInfo.BoxName = boxSession.TargetFile.Name;
                boxSession.EUIUpdtEvt(eBProgressInfo, EventArgs.Empty);

                await BoxCreateBackupXML(boxSession, selBoxes);
                await BoxUpdateLastBackupDate(boxSession,strAdminPaswd, iSelBoxCnt, selBoxes);
                await ZipTargetBackup(boxSession);

                strMsg = Helper.GetResourceString("ID_BACKUP_SUCCESS_MSG");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxBackup() :: Exception Handled: " + ex);
            }
            return strMsg;
        }

        private async static Task BoxCreateBackupXML(BoxSession boxSession, EBBox[] selBoxes)
        {
            try
            {
                XmlDocument backupXML = new XmlDocument();

                XmlDeclaration xmlDeclaration = backupXML.CreateXmlDeclaration("1.0", "UTF-8", String.Empty);
                XmlElement rootNode = GetInsertRootNode(backupXML, Helper.TAGNAME_BACKUPINFO);
                backupXML.InsertBefore(xmlDeclaration, rootNode);
                
                InsertNode(backupXML, rootNode, Helper.TAGNAME_NETBIOSNAME, boxSession.MFPName);
                InsertNode(backupXML, rootNode, Helper.TAGNAME_BACKUPTYPE, "0");
                InsertNode(backupXML, rootNode, Helper.TAGNAME_ENGVERSION, boxSession.EngineInfo.EngineVersion);
                InsertNode(backupXML, rootNode, Helper.TAGNAME_MACADDRESS, boxSession.EngineInfo.MacAddress);

                int iSecMode = (int)boxSession.AuthSettings.SecurityMode;
                InsertNode(backupXML, rootNode, Helper.TAGNAME_SECUREMODE, iSecMode.ToString());
                InsertNode(backupXML, rootNode, Helper.TAGNAME_BACKUPDATE, boxSession.EngineInfo.MfpTime.ToString());
                InsertNode(backupXML, rootNode, Helper.TAGNAME_TIMEZONE, GetTimeZone());


                XmlElement boxesNode = backupXML.CreateElement(Helper.TAGNAME_BOXES);
                boxesNode.SetAttribute(Helper.PROPNAME_NUMBOXES, selBoxes.Length.ToString());
                rootNode.AppendChild(boxesNode);

                foreach (EBBox selBox in selBoxes)
                {
                    EBBoxEx eBBoxEx = GetBoxProperty(boxSession, selBox);

                    if (eBBoxEx != null)
                    {
                        XmlElement boxPropNode = backupXML.CreateElement(Helper.TAGNAME_BOXPROP);
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_BOXNAME, eBBoxEx.BoxName);
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_BOXNO, eBBoxEx.BoxID.ToString());

                        string strIsProtected = eBBoxEx.IsProtected ? Helper.PROTECTED : Helper.NOT_PROTECTED;
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_BOXPROTECT, strIsProtected);

                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_NUMOFFOLDERS, eBBoxEx.NumFolders.ToString());
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_NUMOFDOCS, eBBoxEx.NumDocuments.ToString());
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_BOXUSEDDISK, eBBoxEx.TotalSize.ToString());

                        DateTimeOffset offset = new DateTimeOffset(eBBoxEx.LastBackupDate);
                        InsertNode(backupXML, boxPropNode, Helper.TAGNAME_BOXLASTBACKUP, offset.ToUnixTimeSeconds().ToString());
                        boxesNode.AppendChild(boxPropNode);
                    }
                }

                string strPath = boxSession.TargetFolder.Path + "\\BOX\\" + Helper.BACKUP_FILENAME;
                await Task.Run(() =>
                {
                    File.WriteAllText(strPath, backupXML.OuterXml, Encoding.UTF8);
                });
                backupXML = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxCreateBackupXML() :: Exception Handled: " + ex);
            }
        }

        private static EBBoxEx GetBoxProperty(BoxSession boxSession, EBBox selBox)
        {
            EBBoxEx boxEx = null;
            try
            {
                foreach (EBBoxEx eBBoxEx in boxSession.BoxProperties)
                {
                    if (eBBoxEx.BoxID == selBox.BoxID)
                    {
                        boxEx = eBBoxEx;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: GetBoxProperty() :: Exception Handled: " + ex);
            }
            return boxEx;
        }

        private static string GetTimeZone()
        {
            string strTimeZone = string.Empty;
            char[] separators = new char[] { '(', ')' };
            try
            {
                string[] strTemp = TimeZoneInfo.Local.DisplayName.Split(separators);
                if (strTemp.Length > 0)
                    strTimeZone = strTemp[1].Replace("UTC", "GMT");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: GetTimeZone() :: Exception Handled: " + ex);
            }
            return strTimeZone;
        }

        private static XmlElement GetInsertRootNode(XmlDocument xDoc, string strTag)
        {
            XmlElement rootEle = null;
            try
            {
                rootEle = xDoc.CreateElement(strTag);
                xDoc.AppendChild(rootEle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: GetInsertRootNode() :: Exception Handled: " + ex);
            }
            return rootEle;
        }

        private static void InsertNode(XmlDocument xDoc, XmlElement parentNode, string strTag, string strValue)
        {
            try
            {
                XmlElement childNode = xDoc.CreateElement(strTag);
                childNode.InnerText = strValue;
                parentNode.AppendChild(childNode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: InsertNode() :: Exception Handled: " + ex);
            }
        }

        public async static Task BoxGetBoxProperty(BoxSession boxSession, EBBox[] selBoxes, string strPassword)
        {
            try
            {
                if (boxSession != null && boxSession.MFPSession.BoxSession != 0 && selBoxes.Length != 0)
                {
                    string strParams = string.Empty;
                    HttpResponseMessage responseMessage;

                    foreach (EBBox eBBox in selBoxes)
                    {
                        if (EAccess.BYPASS == boxSession.MFPSession.AdminAccessMode)
                            strPassword = BoxSession.DEFAULT_ADMIN_PASSWORD;

                        strParams = String.Format("?SID={0}&BNO={1}&PWD={2}", boxSession.MFPSession.BoxSession, eBBox.BoxID.ToString("D3"), strPassword);

                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}&BNO={1}&PWD={2}", boxSession.MFPSession.BoxSession, eBBox.BoxID.ToString("D3"), strPassword);


                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWGETBOXPROPERTY, strParams);
                        else
                            responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWGETBOXPROPERTY, strParams);

                        if (EBoxError.S_BOXNW_OK == await IsCommandSuccess(responseMessage))
                        {
                            EBBoxEx eBBoxEx = await Helper.PopulateBoxProperties(responseMessage, boxSession);
                            boxSession.BoxProperties.Add(eBBoxEx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxGetBoxProperty() :: Exception Handled: " + ex);
            }
        }

        private async static Task ZipTargetBackup(BoxSession boxSession)
        {
            try
            {
                string strBkpZipDir = boxSession.TargetFolder.Path + "\\BOX";
                StorageFolder boxFolder = await StorageFolder.GetFolderFromPathAsync(strBkpZipDir);

                await Task.Run(() =>
                {
                    if (File.Exists(boxSession.TargetFile.Path))
                        File.Delete(boxSession.TargetFile.Path);

                    ZipFile.CreateFromDirectory(strBkpZipDir, boxSession.TargetFile.Path, CompressionLevel.Optimal, true);
                });

                await boxFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: ZipTargetBackup() :: Exception Handled: " + ex);
            }
        }

        private async static Task BoxUpdateLastBackupDate(BoxSession boxSession, string strAdminPaswd, int iSelBoxCnt, EBBox[] selBoxes)
        {
            try
            {
                if (boxSession.MFPSession != null && boxSession.MFPSession.BoxSession != 0 && EAccess.RESTRICTED != boxSession.MFPSession.AdminAccessMode)
                {
                    string strParams = string.Empty;
                    HttpResponseMessage responseMessage;

                    string strBoxList = string.Empty;
                    char[] strBoxSep = new char[] { ',' };

                    foreach (EBBox box in selBoxes)
                        strBoxList += strBoxSep[0] + box.BoxID.ToString("D3");

                    strBoxList = strBoxList.TrimStart(strBoxSep);

                    DateTimeOffset offset = new DateTimeOffset(DateTime.Now);
                    long lCurBackupTime = offset.ToUnixTimeSeconds();

                    if (EAccess.EBX == boxSession.MFPSession.AdminAccessMode)
                        strParams = String.Format("SID={0}&LBD={1}&TNB={2}&BNO={3}", boxSession.MFPSession.BoxSession, lCurBackupTime, iSelBoxCnt, strBoxList);
                    else
                    {
                        if (EAccess.BYPASS == boxSession.MFPSession.AdminAccessMode)
                            strAdminPaswd = BoxSession.DEFAULT_ADMIN_PASSWORD;

                        strParams = String.Format("SID={0}&PWD={1}&LBD={2}&TNB={3}&BNO={4}", boxSession.MFPSession.BoxSession, strAdminPaswd, lCurBackupTime, iSelBoxCnt, strBoxList);
                    }

                    responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWUPDATELASTBACKUPDATE, strParams);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxUpdateLastBackupDate() :: Exception Handled: " + ex);
            }
        }

        private async static Task BoxZip(BoxSession boxSession, EBBox ebBox)
        {
            string strZipFile = string.Empty;
            string strZipFolder = string.Empty;
            string strEfbFile = string.Empty;

            StorageFile boxZipFile = null;
            StorageFolder boxZipFldr = null;
            StorageFolder boxFolder = null;
            try
            {
                if (BoxSession.PUBLIC_BOX == ebBox.BoxID)
                {
                    strZipFile = String.Format("{0}\\BOX\\{1}.zip", boxSession.TargetFolder.Path, ebBox.BoxID.ToString("D5"));
                    strZipFolder = String.Format("{0}\\BOX\\{1}", boxSession.TargetFolder.Path, ebBox.BoxID.ToString("D5"));
                    strEfbFile = String.Format("{0}.efb", ebBox.BoxID.ToString("D5"));
                }
                else
                {
                    strZipFile = String.Format("{0}\\BOX\\{1}.zip", boxSession.TargetFolder.Path, ebBox.BoxID.ToString("D3"));
                    strZipFolder = String.Format("{0}\\BOX\\{1}", boxSession.TargetFolder.Path, ebBox.BoxID.ToString("D3"));
                    strEfbFile = String.Format("{0}.efb", ebBox.BoxID.ToString("D3"));
                }

                await Task.Run(() =>
                {
                   ZipFile.CreateFromDirectory(strZipFolder, strZipFile, CompressionLevel.Optimal, true);
                });
                
                boxZipFile = await StorageFile.GetFileFromPathAsync(strZipFile);
                boxZipFldr = await StorageFolder.GetFolderFromPathAsync(strZipFolder);

                string strBoxFldr = String.Format("{0}\\BOX\\", boxSession.TargetFolder.Path);
                boxFolder = await StorageFolder.GetFolderFromPathAsync(strBoxFldr);

                await boxZipFile.MoveAsync(boxFolder, strEfbFile, NameCollisionOption.ReplaceExisting);
                await boxZipFldr.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: ZipBox() :: Exception Handled: " + ex);
            }
        }

        private async static Task BoxDownload(BoxSession boxSession, EBBox ebBox, EBProgressInfo eBProgressInfo)
        {
            string strSrc = string.Empty;
            string strDest = string.Empty;
            try
            {
                if (BoxSession.PUBLIC_BOX == ebBox.BoxID)
                {
                    strSrc = ebBox.BoxID.ToString("D5");
                    strDest = "\\BOX\\" + ebBox.BoxID.ToString("D5");
                }
                else
                {
                    strSrc = ebBox.BoxID.ToString("D3");
                    strDest = "\\BOX\\" + ebBox.BoxID.ToString("D3");
                }

                CreateDirectories(boxSession.TargetFolder, strDest);
                strDest = boxSession.TargetFolder.Path + "\\BOX";

                await FTPWrapper.Download(boxSession, strDest, strSrc, eBProgressInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: DownloadBox() :: Exception Handled: " + ex);
            }
        }

        private async static void CreateDirectories(StorageFolder targetFldr, string strDest)
        {
            try
            {
                char[] separators = new char[] { '\\' };
                string[] stDirs = strDest.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                StorageFolder temp = targetFldr;

                foreach (string strDir in stDirs)
                    temp = await temp.CreateFolderAsync(strDir, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: CreateDirectories() :: Exception Handled: " + ex);
            }
        }

        public async static Task BoxDeleteFTPAccount(BoxSession boxSession, string strPassword)
        {
            try
            {
                if (boxSession.MFPSession != null && boxSession.MFPSession.BoxSession != 0 && EAccess.RESTRICTED != boxSession.MFPSession.AdminAccessMode)
                {
                    string strParams = string.Empty;
                    HttpResponseMessage responseMessage;

                    if (EAccess.EBX == boxSession.MFPSession.AdminAccessMode)
                    {
                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}", boxSession.MFPSession.BoxSession);
                        else
                            strParams = String.Format("?SID={0}", boxSession.MFPSession.BoxSession);
                    }
                    else
                    {
                        if (EAccess.BYPASS == boxSession.MFPSession.AdminAccessMode)
                            strPassword = BoxSession.DEFAULT_ADMIN_PASSWORD;


                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                        else
                            strParams = String.Format("?SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                    }

                    if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWDELETEFTPACCOUNT, strParams);
                    else
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWDELETEFTPACCOUNT, strParams);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxDeleteFTPAccount() :: Exception Handled: " + ex);
            }
        }

        public async static Task BoxCloseConnection(BoxSession boxSession)
        {
            try
            {
                if (boxSession.MFPSession != null && boxSession.MFPSession.BoxSession != 0 && EAccess.RESTRICTED != boxSession.MFPSession.AdminAccessMode)
                {
                    string strParams = string.Empty;
                    HttpResponseMessage responseMessage;

                    if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                    {
                        strParams = String.Format("SID={0}", boxSession.MFPSession.BoxSession);
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWDISCONNECT, strParams);
                    }
                    else
                    {
                        strParams = String.Format("?SID={0}", boxSession.MFPSession.BoxSession);
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWDISCONNECT, strParams);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxCloseConnection() :: Exception Handled: " + ex);
            }
        }

        public async static Task BoxGetEngineInfo(BoxSession boxSession, string strPassword)
        {
            try
            {
                if (boxSession != null && boxSession.MFPSession != null && boxSession.MFPSession.BoxSession != 0)
                {
                    HttpResponseMessage responseMessage = null;
                    string strParams = string.Empty;

                    if (EAccess.EBX == boxSession.MFPSession.AdminAccessMode)
                    {
                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}", boxSession.MFPSession.BoxSession);
                        else
                            strParams = String.Format("?SID={0}", boxSession.MFPSession.BoxSession);
                    }
                    else
                    {
                        if (EAccess.BYPASS == boxSession.MFPSession.AdminAccessMode)
                            strPassword = BoxSession.DEFAULT_ADMIN_PASSWORD;

                        if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                            strParams = String.Format("SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                        else
                            strParams = String.Format("?SID={0}&PWD={1}", boxSession.MFPSession.BoxSession, strPassword);
                    }


                    if (boxSession.EngineInfo.EngineVersion.Contains(Helper.VERSION2))
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.POST, boxSession.MFPSession, BoxSession.CGI_BOXNWGETENGINEVERSION, strParams);
                    else
                        responseMessage = await SendAndGetHTTPCmdResp(EHTTPReqType.GET, boxSession.MFPSession, BoxSession.CGI_BOXNWGETENGINEVERSION, strParams);

                    if (EBoxError.S_BOXNW_OK == await IsCommandSuccess(responseMessage))
                    {
                        boxSession.EngineInfo.MacAddress = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_MACADDRESS);

                        string strMFPTime = await Helper.GetNodeValue(responseMessage, Helper.TAGNAME_CURRENTTIME);
                        long lMFPTime = !string.IsNullOrEmpty(strMFPTime) ? Convert.ToInt64(strMFPTime) : default(long);
                        boxSession.EngineInfo.MfpTime = DateTimeOffset.FromUnixTimeSeconds(lMFPTime).LocalDateTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BoxInterface :: BoxGetEngineInfo() :: Exception Handled: " + ex);
            }
        }
    }
}
