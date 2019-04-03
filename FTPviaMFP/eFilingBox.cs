using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml.Media;

namespace FTPviaMFP
{
    public enum EBoxError
    {
        S_BOXNW_OK,
        S_BOXNW_SYS_MALLOC_ERROR,
        S_BOXNW_SYS_INVALID_ADMINPASS,

        /*:::::::::::::::::::::::For eBX:::::::::::::::::::::::*/
        S_BOXNW_SYS_FUNCTION_ERROR = 4,

        S_BOXNW_WEB_NO_SESSION = 100,
        S_BOXNW_WEB_PARAMETER_ERROR,
        S_BOXNW_WEB_SESSION_INIT_ERROR,
        S_BOXNW_WEB_SESSION_WRITE_ERROR,
        S_BOXNW_WEB_SESSION_READ_ERROR,
        S_BOXNW_WEB_EX_ERROR,
        // added since 1.0.6.2
        S_BOXNW_WEB_SESSION_TIMEOUT_UPDATE_ERROR,
        // added since 1.0.6.3
        S_BOXNW_WEB_SESSION_TIMEOUT_UPDATE_END_ERROR,

        S_BOXNW_BOX_FATAL_ERROR = 200,
        S_BOXNW_BOX_IS_NOT_READY,
        S_BOXNW_BOX_NO_SESSION,
        S_BOXNW_BOX_END_IMPOSSIBLE,
        S_BOXNW_BOX_ACCESS_ERROR,
        S_BOXNW_BOX_STS_UPDATE,
        S_BOXNW_BOX_DISK_FULL,
        S_BOXNW_BOX_DELETE_IMPOSSIBLE,
        S_BOXNW_BOX_LOCK_IMPOSSIBLE,
        S_BOXNW_BOX_UNLOCK_IMPOSSIBLE,
        S_BOXNW_BOX_INVALID_BOX_PASS,
        S_BOXNW_BOX_BOXISUSED,
        S_BOXNW_BOX_BACKUP_RESTORE_MODE,
        S_BOXNW_BOX_LASTUPDATE_ERROR,
        S_BOXNW_BOX_IS_DISABLE,

        S_BOXNW_PANEL_EX_ERROR = 300,
        S_BOXNW_JOB_EX_ERROR = 400,

        S_BOXNW_NIC_FTP_CREATE_ERROR = 500,
        S_BOXNW_NIC_FTP_DELETE_ERROR,

        S_BOXNW_AUTH_PASSWORD_RESET = 602,		// User Authentication Failure by Password Reset
        S_BOXNW_AUTH_PASSWORD_EXPIRED,          // User Authentication Failure by Password Expired
        S_BOXNW_AUTH_POLICY_EXPIRED,		    // User Authentication Failure by Password Policy Expired
        S_BOXNW_AUTH_ACCOUNT_LOCKED,		    // User Authentication Failure by Account Locked

        FAIL = 999
    }

    public enum EState
    {
        ENABLED = 1,
        DISABLED
    }

    public enum EAccess
    {
        ALLOWED,
        BYPASS,
        RESTRICTED,
        EBX = 20
    }

    public enum ESecurityMode
    {
        NORMAL = 1,
        P2600
    }

    public enum EAuthMethod
    {
        DISABLE = 0,
        USRLOCAL = 2,
        USRLDAP,
        USRWINDMN,
        USRKERB,
        USRNWARE
    }

    public enum EHashAlgo
    {
        MD5 = 1,
        SHA1,
        SHA256
    }

    public enum EHTTPReqType
    {
        GET,
        POST
    }

    public enum EOperation
    {
        PROCESSING,
        DOWNLOADING,
        UPLOADING,
        ZIPPING,
        UNZIPPING,
        CLEANING,
        ABORTING,
        SKIPPING
    }

    public class BoxSession
    {
        public const string CGI_BOXNWCONNECT = "/e-FilingBox/boxnwConnect";
        public const string CGI_BOXNWGETSECURITYSETTING = "/e-FilingBox/boxnwGetSecuritySetting";
        public const string CGI_BOXNWAUTHENTICATE = "/e-FilingBox/boxnwAuthenticate";
        public const string CGI_BOXNWGETBOXLIST = "/e-FilingBox/Admin/boxnwGetBoxList";
        public const string CGI_BOXNWCREATEFTPACCOUNT = "/e-FilingBox/Admin/boxnwCreateFTPAccount";
        public const string CGI_BOXNWUPDATELASTBACKUPDATE = "/e-FilingBox/Admin/boxnwChangeLastBackupDate";
        public const string CGI_BOXNWDELETEFTPACCOUNT = "/e-FilingBox/Admin/boxnwDeleteFTPAccount";
        public const string CGI_BOXNWDISCONNECT = "/e-FilingBox/boxnwDisconnect";
        public const string CGI_BOXNWGETBOXPROPERTY = "/e-FilingBox/Admin/boxnwGetBoxProperty";
        public const string CGI_BOXNWGETENGINEVERSION = "/e-FilingBox/Admin/boxnwGetEngineVersion";

        public const int PUBLIC_BOX = 0;
        public const string DEFAULT_ADMIN_PASSWORD = "";

        public string MFPName { get; set; }
        public EBMFPSession MFPSession { get; set; }
        public EBEngineInfo EngineInfo { get; set; }
        public EBAuthSettings AuthSettings { get; set; }

        public List<EBBox> Boxes = null;
        public List<EBBoxEx> BoxProperties = null;

        public ulong BoxCount { get; set; }

        public StorageFolder TargetFolder { get; set; }
        public StorageFile TargetFile { get; set; }

        public EventHandler EUIUpdtEvt { get; set; }

        public BoxSession()
        {
            MFPSession = new EBMFPSession();
            EngineInfo = new EBEngineInfo();
            AuthSettings = new EBAuthSettings();

            BoxProperties = new List<EBBoxEx>();
        }
    }

    public class EBMFPSession
    {
        public string ServerName { get; set; }
        public int ServerPort { get; set; }
        public int HTTPServerPort { get; set; }
        public int FTPServerPort { get; set; }
        public string HTTPUserName { get; set; }
        public string HTTPPassword { get; set; }
        public ulong HTTPTimeOut { get; set; }
        //HINTERNET hMfpSession;
        //HANDLE hMfpConnection;
        public ulong BoxSession { get; set; }
        public EAccess AdminAccessMode { get; set; }
        public EState HTTPSSLEnabled { get; set; }
        public EState FTPSSLEnabled { get; set; }
        public bool IsValidSession { get; set; }
        public ulong ScopeId { get; set; }
    }

    public class EBEngineInfo
    {
        public string EngineVersion { get; set; }
        public string MacAddress { get; set; }
        public DateTime MfpTime { get; set; }
    }

    public class EBAuthSettings
    {
        public ESecurityMode SecurityMode { get; set; }
        public bool AuthState { get; set; }
        public EAuthMethod AuthMode { get; set; }
        public bool Encryption { get; set; }
        public EHashAlgo HashAlgo { get; set; }
        public string HTTPUserName { get; set; }
        public string HTTPPassword { get; set; }
        public string HTTPDomain { get; set; }
        public string HTTPUserDept { get; set; }
    }

    public class EBBox
    {
        public ulong BoxID { get; set; }
        public string BoxName { get; set; } // Box name set by user
        public DateTime ModificationDate { get; set; }   // Date and time when this Box is last modified
        public bool BoxStatus { get; set; }
        public bool IsProtected { get; set; }
        public DateTime LastBackupDate { get; set; }    //Newly Added for Incremental BK
        public string UILastBackupDate { get; set; }
        public ImageSource BoxIcon { get; set; }
    }

    public class EBBoxEx
    {
        public ulong BoxID { get; set; }
        public string BoxName { get; set; }
        public string Owner { get; set; }
        public string SrcPasswordDir { get; set; }
        public string DestPasswordDir { get; set; }
        public DateTime LastBackupDate { get; set; }
        public DateTime CreationDate { get; set; }
        public bool BoxStatus { get; set; }
        public bool IsProtected { get; set; }

        public ulong NumDocuments { get; set; }
        public ulong NumFolders { get; set; }
        public DateTime LastModified { get; set; }
        public ulong TotalSize { get; set; }
        public ulong PreservationPeriod { get; set; }
        public string OwnerEmailAddress { get; set; }
    }

    public class EBProgressInfo
    {
        public ulong PercentageDone { get; set; }
        public ulong DocumentsTransfered { get; set; }
        public ulong TotalDocuments { get; set; }
        public string BoxName { get; set; }
        public string FolderName { get; set; }
        public string DocName { get; set; }
        public EOperation OperationInfo { get; set; }
        public ulong DeleteDocFlag { get; set; }
        public ulong DeleteDocOption { get; set; }
    }
}
