using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace FTPviaMFP
{
    public class HTTPWrapper
    {
        const string HTTP_PREFIX = "http://";
        const string IP_PORT_SEPARATOR = ":";

        private static HttpClient httpClient = new HttpClient();

        public async static Task<HttpResponseMessage> GetRequest(EBMFPSession eBMFPSession, string strURL, string strParams)
        {
            HttpResponseMessage responseMessage = null;
            try
            {
                string stURL = HTTP_PREFIX + eBMFPSession.ServerName + IP_PORT_SEPARATOR + eBMFPSession.ServerPort + strURL + strParams;
                Uri reqURI = new Uri(stURL);

                responseMessage = new HttpResponseMessage();
                responseMessage = await httpClient.GetAsync(reqURI);
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HTTPWrapper :: GetRequest() :: Exception Handled: " + ex);
            }
            return responseMessage;
        }

        public async static Task<HttpResponseMessage> PostRequest(EBMFPSession eBMFPSession, string strURL, string strParams)
        {
            HttpResponseMessage responseMessage = null;
            try
            {
                string stURL = HTTP_PREFIX + eBMFPSession.ServerName + IP_PORT_SEPARATOR + eBMFPSession.ServerPort + strURL;
                Uri reqURI = new Uri(stURL);

                responseMessage = new HttpResponseMessage();
                responseMessage = await httpClient.PostAsync(reqURI, new HttpStringContent(strParams));
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HTTPWrapper :: PostRequest() :: Exception Handled: " + ex);
            }
            return responseMessage;
        }
    }
}
