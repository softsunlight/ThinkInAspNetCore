using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ThinkInAspNetCore
{
    /// <summary>
    /// http请求处理类
    /// </summary>
    public class HttpHandler
    {
        public HttpRequest httpRequest { get; set; }
        public HttpResponse httpResponse { get; set; }

        public HttpHandler()
        {

        }

        public HttpHandler(HttpRequest httpRequest) : this()
        {
            this.httpRequest = httpRequest;
            this.httpResponse = new HttpResponse();
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        public void ProcessRequest()
        {
            try
            {
                httpResponse.Version = httpRequest == null ? "HTTP/1.1" : httpRequest.Version;
                httpResponse.ResponseStream = httpRequest.RequestStream;
                string webDir = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = string.Empty;
                if (httpRequest.RequstUrl == "/")
                {
                    filePath = Path.Combine(webDir, "index.html");
                    httpResponse.ContentType = "text/html";
                }
                else
                {
                    filePath = Path.Combine(webDir, httpRequest.RequstUrl.Remove(0, 1));
                }
                if (!File.Exists(filePath))
                {
                    httpResponse.StatusCode = "404";
                    httpResponse.StatusMessage = "NotFound";
                }
                else
                {
                    httpResponse.StatusCode = "200";
                    httpResponse.StatusMessage = "OK";
                    httpResponse.ResponseBody = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                httpResponse.Write();
            }
        }

    }
}
