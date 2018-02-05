using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace AzureChaos.ReguestHelpers
{
    public class HTTPHelpers
    {
        public static string ExecuteGetWebRequest(string webReqURL)
        {
            string result = string.Empty;
            try
            {
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(webReqURL);
                httpWebRequest.Proxy = null;
                httpWebRequest.Method = WebRequestMethods.Http.Get;
                HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
                    result = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                //ErrorLogger.Log(string.Format("Web exception occured while making HTTP call. Url - {0}, PostJson - {1}, Message - {2}, Stack Trace - {3}", url, postJson, ex.Message, ex.StackTrace), LogLevel.ElasticSearch);
            }
            return result;
        }

        public static string ExecuteESPostJsonWebRequest(string url, string postJson)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Credentials = new NetworkCredential("","");
            request.Timeout = 600;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            WebResponse response = null;
            Stream stream = null;
            StreamReader reader = null;
            string result = string.Empty;
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(postJson);
                request.ContentLength = bytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                response = request.GetResponse();
                stream = response.GetResponseStream();
                reader = new StreamReader(stream);
                result = reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                //ErrorLogger.Log(string.Format("Web exception occured while making HTTP call. Url - {0}, PostJson - {1}, Message - {2}, Stack Trace - {3}", url, postJson, ex.Message, ex.StackTrace), LogLevel.ElasticSearch);
            }
            catch (Exception ex)
            {
                //ErrorLogger.Log(string.Format("Web exception occured while making HTTP call. Url - {0}, PostJson - {1}, Message - {2}, Stack Trace - {3}", url, postJson, ex.Message, ex.StackTrace), LogLevel.ElasticSearch);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader.Close();
                }
                if (response != null)
                {
                    response.Close();
                    response.Dispose();
                }
            }
            return result;
        }
    }
}
