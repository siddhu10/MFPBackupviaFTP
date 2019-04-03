using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace FTPviaMFP
{
    class Helper
    {
        public const string APPLN_NAME = "BKRT";

        public const string TAGNAME_RESULT = "Result";
        public const string TAGNAME_ENGINEVERSION = "Version";
        public const string TAGNAME_SESSIONID = "WebSessionID";
        public const string TAGNAME_ACCESSMODE = "AccessMode";
        public const string TAGNAME_SECURITYMODE = "SecurityMode";
        public const string TAGNAME_AUTHSTATE = "AuthenticationEnable";
        public const string TAGNAME_AUTHMODE = "AuthenticationMethod";
        public const string TAGNAME_HASHALGO = "HashAlgorithm";
        public const string TAGNAME_DOMAINS = "AuthenticationDomainName";
        public const string TAGNAME_AUTHORIZE = "Authorize";
        public const string TAGNAME_WebSessionID = "WebSessionID";
        public const string TAGNAME_BOXES = "Boxes";
        public const string TAGNAME_BOXPROP = "BoxProp";
        public const string TAGNAME_BOXNAME = "BoxName";
        public const string TAGNAME_LASTUPDATEDATE = "LastUpdateDate";
        public const string TAGNAME_BOXPROTECT = "Protected";
        public const string TAGNAME_BOXLASTBACKUP = "LastBackupDate";
        public const string TAGNAME_LOGINNAME = "LoginName";
        public const string TAGNAME_LOGINPASSWORD = "LoginPassword";
        public const string TAGNAME_NUMOFFOLDERS = "NumOfFolders";
        public const string TAGNAME_NUMOFDOCS = "NumOfDocs";
        public const string TAGNAME_PRESVPERIODFLAG = "PreservationPeriodFlag";
        public const string TAGNAME_BOXUSEDDISK = "BoxUsedDisk";
        public const string TAGNAME_PRESVPERIOD = "PreservationPeriod";
        public const string TAGNAME_WARNGNOTICE = "WarningNotice";
        public const string TAGNAME_CREATEDATE = "CreateDate";
        public const string TAGNAME_BOXOWNER = "BoxOwner";
        public const string TAGNAME_MACADDRESS = "MACAddress";
        public const string TAGNAME_CURRENTTIME = "CurrentTime";
        public const string TAGNAME_BACKUPINFO = "BackupInfo";
        public const string TAGNAME_NETBIOSNAME = "NetBiosName";
        public const string TAGNAME_BACKUPTYPE = "BackupType";
        public const string TAGNAME_ENGVERSION = "EngineVersion";
        public const string TAGNAME_SECUREMODE = "SecureMode";
        public const string TAGNAME_BACKUPDATE = "BackupDate";
        public const string TAGNAME_TIMEZONE = "Timezone";
        public const string TAGNAME_BOXNO = "BoxNo";

        public const string PROPNAME_NUMBOXES = "NumOfBoxes";
        public const string PROPNAME_BOXNO = "BoxNo";

        public const string VERSION2 = "V2.0.0.0";
        public const string BACKUP_FILENAME = "Backup.xml";
        public const string PROTECTED = "yes";
        public const string NOT_PROTECTED = "no";

        public const char NODE_SEPARATOR = ':';


        private static ResourceLoader loader = new ResourceLoader();

        public static string GetResourceString(string strID)
        {
            string strValue = string.Empty;
            if (loader != null)
                strValue = loader.GetString(strID);
            return strValue;
        }

        public static ContentDialog GetDialog()
        {
            ContentDialog chkDialog = new ContentDialog();
            try
            {
                chkDialog.Title = GetResourceString("IDS_CHKDLG_TITLE");
                chkDialog.PrimaryButtonText = GetResourceString("IDS_CHKDLG_OK");
                chkDialog.SecondaryButtonText = GetResourceString("IDS_CHKDLG_CLOSE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: GetDialog() :: Exception Handled: " + ex);
            }
            return chkDialog;
        }

        public async static Task<string> GetNodeValue(HttpResponseMessage responseMessage, string strTagName)
        {
            string strValue = string.Empty;
            try
            {  
                string strResponse = await responseMessage.Content.ReadAsStringAsync();

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(strResponse);

                XmlNodeList xmlNodeList = xDoc.GetElementsByTagName(strTagName);
                if (xmlNodeList != null && xmlNodeList.Count > 0)
                {
                    if (xmlNodeList.Count == 1)
                        strValue = xmlNodeList[0].FirstChild.InnerText;
                    else
                        strValue = GetAllNodeValues(xmlNodeList);
                }
                xDoc = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: GetNodeValue() :: Exception Handled: " + ex);
            }
            return strValue;
        }

        static string GetAllNodeValues(XmlNodeList xmlNodeList)
        {
            string strValue = string.Empty;
            try
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                    strValue += xmlNode.InnerText + NODE_SEPARATOR;

                strValue.TrimEnd(new char[] { NODE_SEPARATOR });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: GetAllNodeValues() :: Exception Handled: " + ex);
            }
            return strValue;
        }

        public async static Task<string> GetPropValue(HttpResponseMessage responseMessage, string strTagName, string strPropName)
        {
            string strValue = string.Empty;
            try
            {
                string strResponse = await responseMessage.Content.ReadAsStringAsync();

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(strResponse);

                XmlNodeList xmlNodeList = xDoc.GetElementsByTagName(strTagName);
                if (xmlNodeList != null && xmlNodeList.Count > 0)
                {
                    XmlElement xmlEle = xmlNodeList[0] as XmlElement;
                    strValue = xmlEle.HasAttribute(strPropName) ? xmlEle.GetAttribute(strPropName) : string.Empty;
                }
                xDoc = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: GetPropValue() :: Exception Handled: " + ex);
            }
            return strValue;
        }

        public async static Task PopulateBoxes(HttpResponseMessage responseMessage, BoxSession boxSession)
        {
            try
            {
                string strResponse = await responseMessage.Content.ReadAsStringAsync();

                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(strResponse);

                XmlNodeList xmlNodeList = xDoc.GetElementsByTagName(TAGNAME_BOXPROP);
                if (xmlNodeList != null && xmlNodeList.Count > 0)
                {
                    foreach (XmlNode xmlNode in xmlNodeList)
                    {
                        EBBox eBBox = new EBBox();

                        XmlElement xmlEle = xmlNode as XmlElement;
                        string strBoxID = xmlEle.HasAttribute(PROPNAME_BOXNO) ? xmlEle.GetAttribute(PROPNAME_BOXNO) : string.Empty;
                        eBBox.BoxID = Convert.ToUInt64(strBoxID);

                        eBBox.BoxStatus = true;


                        XmlNodeList nodeList = null;
                        nodeList = xmlEle.GetElementsByTagName(TAGNAME_BOXNAME);
                        eBBox.BoxName = nodeList != null && nodeList.Count > 0 ? nodeList[0].InnerText : string.Empty;

                        nodeList = xmlEle.GetElementsByTagName(TAGNAME_BOXPROTECT);
                        eBBox.IsProtected = nodeList != null && nodeList.Count > 0 ? Convert.ToInt32(nodeList[0].InnerText) == 1 ? true : false : false;

                        nodeList = xmlEle.GetElementsByTagName(TAGNAME_LASTUPDATEDATE);
                        long lUpdateTime = nodeList != null && nodeList.Count > 0 ? Convert.ToInt64(nodeList[0].InnerText) : default(long);
                        eBBox.ModificationDate = DateTimeOffset.FromUnixTimeSeconds(lUpdateTime).LocalDateTime;

                        nodeList = xmlEle.GetElementsByTagName(TAGNAME_BOXLASTBACKUP);
                        long lBackupTime = nodeList != null && nodeList.Count > 0 ? Convert.ToInt64(nodeList[0].InnerText) : default(long);
                        eBBox.LastBackupDate = DateTimeOffset.FromUnixTimeSeconds(lBackupTime).LocalDateTime;
                        eBBox.UILastBackupDate = lBackupTime != 0 ? eBBox.LastBackupDate.ToString() : "-- -- --  -- -- --";

                        boxSession.Boxes.Add(eBBox);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: PopulateBoxes() :: Exception Handled: " + ex);
            }
        }

        public async static Task<EBBoxEx> PopulateBoxProperties(HttpResponseMessage responseMessage, BoxSession boxSession)
        {
            EBBoxEx eBBoxEx = new EBBoxEx();
            XmlNodeList nodeList = null;
            try
            {
                string strResponse = await responseMessage.Content.ReadAsStringAsync();

                string strBoxNo = await GetPropValue(responseMessage, TAGNAME_BOXPROP, PROPNAME_BOXNO);
                eBBoxEx.BoxID = !string.IsNullOrEmpty(strBoxNo) ? Convert.ToUInt64(strBoxNo) : default(ulong);

                eBBoxEx.BoxStatus = true;


                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(strResponse);

                nodeList = xDoc.GetElementsByTagName(TAGNAME_BOXNAME);
                eBBoxEx.BoxName = nodeList != null && nodeList.Count > 0 ? nodeList[0].InnerText : string.Empty;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_BOXPROTECT);
                eBBoxEx.IsProtected = nodeList != null && nodeList.Count > 0 ? Convert.ToInt32(nodeList[0].InnerText) == 1 ? true : false : false;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_NUMOFFOLDERS);
                eBBoxEx.NumFolders = nodeList != null && nodeList.Count > 0 ? Convert.ToUInt64(nodeList[0].InnerText) : default(ulong);

                nodeList = xDoc.GetElementsByTagName(TAGNAME_NUMOFDOCS);
                eBBoxEx.NumDocuments = nodeList != null && nodeList.Count > 0 ? Convert.ToUInt64(nodeList[0].InnerText) : default(ulong);

                nodeList = xDoc.GetElementsByTagName(TAGNAME_PRESVPERIODFLAG);
                bool iPresvPerdFlag = nodeList != null && nodeList.Count > 0 ? Convert.ToInt32(nodeList[0].InnerText) == 1 ? true : false : false;

                if (iPresvPerdFlag)
                {
                    nodeList = xDoc.GetElementsByTagName(TAGNAME_PRESVPERIOD);
                    eBBoxEx.PreservationPeriod = nodeList != null && nodeList.Count > 0 ? Convert.ToUInt64(nodeList[0].InnerText) : default(ulong);
                }
                else
                    eBBoxEx.PreservationPeriod = 0;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_BOXUSEDDISK);
                eBBoxEx.TotalSize = nodeList != null && nodeList.Count > 0 ? Convert.ToUInt64(nodeList[0].InnerText) : default(ulong);

                nodeList = xDoc.GetElementsByTagName(TAGNAME_WARNGNOTICE);
                eBBoxEx.OwnerEmailAddress = nodeList != null && nodeList.Count > 0 ? nodeList[0].InnerText : string.Empty;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_CREATEDATE);
                long lCrtTime = nodeList != null && nodeList.Count > 0 ? Convert.ToInt64(nodeList[0].InnerText) : default(long);
                eBBoxEx.CreationDate = DateTimeOffset.FromUnixTimeSeconds(lCrtTime).LocalDateTime;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_LASTUPDATEDATE);
                long lUpdtTime = nodeList != null && nodeList.Count > 0 ? Convert.ToInt64(nodeList[0].InnerText) : default(long);
                eBBoxEx.LastModified = DateTimeOffset.FromUnixTimeSeconds(lUpdtTime).LocalDateTime;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_BOXLASTBACKUP);
                long lBkpDate = nodeList != null && nodeList.Count > 0 ? Convert.ToInt64(nodeList[0].InnerText) : default(long);
                eBBoxEx.LastBackupDate = DateTimeOffset.FromUnixTimeSeconds(lBkpDate).LocalDateTime;

                nodeList = xDoc.GetElementsByTagName(TAGNAME_BOXOWNER);
                eBBoxEx.Owner = nodeList != null && nodeList.Count > 0 ? nodeList[0].InnerText : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: PopulateAllBoxProperties() :: Exception Handled: " + ex);
            }
            return eBBoxEx;
        }

        public static string GetOperationString(EOperation eOperation)
        {
            string strOperation = eOperation.ToString();
            try
            {
                switch (eOperation)
                {
                    case EOperation.ABORTING:
                        strOperation = GetResourceString("ID_ABORTING");
                        break;

                    case EOperation.CLEANING:
                        strOperation = GetResourceString("ID_CLEANING");
                        break;

                    case EOperation.DOWNLOADING:
                        strOperation = GetResourceString("ID_DOWNLOADING");
                        break;

                    case EOperation.PROCESSING:
                        strOperation = GetResourceString("ID_PROCESSING");
                        break;

                    case EOperation.SKIPPING:
                        strOperation = GetResourceString("ID_SKIPPING");
                        break;

                    case EOperation.UNZIPPING:
                        strOperation = GetResourceString("ID_UNZIPPING");
                        break;

                    case EOperation.UPLOADING:
                        strOperation = GetResourceString("ID_UPLOADING");
                        break;

                    case EOperation.ZIPPING:
                        strOperation = GetResourceString("ID_ZIPPING");
                        break;

                    default:
                        strOperation = GetResourceString("ID_" + eOperation.ToString());
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Helper :: GetOperationString() :: Exception Handled: " + ex);
            }
            return strOperation;
        }
    }
}