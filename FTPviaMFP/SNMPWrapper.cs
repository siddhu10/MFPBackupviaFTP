using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace FTPviaMFP
{
    public static class SNMPWrapper
    {
        //HTTP related OIDs
        public const string OID_HTTP_PORTNO = "1.3.6.1.4.1.1129.2.3.50.1.6.1.4.11.1.1.6.1.1";
        public const string OID_HTTP_SSLPORTNO = "1.3.6.1.4.1.1129.2.3.50.1.6.1.23.1.1.3.1.1";
        public const string OID_HTTP_SSLPORTSTATE = "1.3.6.1.4.1.1129.2.3.50.1.6.1.23.1.1.2.1.1";

        //FTP related OIDs
        public const string OID_FTP_PORTNO = "1.3.6.1.4.1.1129.2.3.50.1.6.1.4.13.1.1.3.1.1";
        public const string OID_FTP_SSLPORTNO = "1.3.6.1.4.1.1129.2.3.50.1.6.1.23.3.1.3.1.1";
        public const string OID_FTP_SSLPORTSTATE = "1.3.6.1.4.1.1129.2.3.50.1.6.1.23.3.1.2.1.1";

        public const string OID_MFP_NAME = "1.3.6.1.2.1.43.5.1.1.16.1";

        const int SNMP_PORT = 161;
        const string COMMUNITY = "public";
        const int TIMEOUT = 5000;

        public static Task<string> GetOIDValue(string strServerName, string strOID)
        {
            return Task.Run(() =>
            {
                string strValue = string.Empty;
                try
                {
                    IPAddress strMFPIP = IPAddress.Parse(strServerName);
                    Variable variable = new Variable(new ObjectIdentifier(strOID));

                    List<Variable> lstVars = new List<Variable>() { variable };
                    IList<Variable> lResult = Messenger.Get(VersionCode.V1 | VersionCode.V2, new IPEndPoint(strMFPIP, SNMP_PORT), new OctetString(COMMUNITY), lstVars, TIMEOUT);

                    if (lResult != null && lResult.Count > 0)
                    {
                        foreach (Variable var in lResult)
                        {
                            strValue = var.Id.ToString() == strOID ? var.Data.ToString() : string.Empty;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("SNMPWrapper :: GetOIDValue() :: Exception Handled: " + ex);
                }
                return strValue;
            });
        }
    }
}
