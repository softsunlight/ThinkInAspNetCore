using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkInAspNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            StartWebServer();
        }

        /// <summary>
        /// 启动web服务
        /// </summary>
        private static void StartWebServer()
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
                tcpListener.Start();
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    Task.Run(() =>
                    {
                        HttpHandler httpHandler = null;
                        try
                        {
                            NetworkStream networkStream = tcpClient.GetStream();
                            MemoryStream memoryStream = new MemoryStream();
                            int recvTotals = 0;
                            while (tcpClient.Available > 0)
                            {
                                byte[] buffer = new byte[512];
                                int realLength = networkStream.Read(buffer, 0, buffer.Length);
                                memoryStream.Write(buffer, 0, realLength);
                                recvTotals += realLength;
                            }
                            if (recvTotals > 0)
                            {
                                string content = Encoding.UTF8.GetString(memoryStream.ToArray());
                                Log.Write(content);
                                if (memoryStream != null)
                                {
                                    memoryStream.Close();
                                    memoryStream.Dispose();
                                    memoryStream = null;
                                }
                                HttpRequest httpRequest = BuildRequest(tcpClient, content);
                                httpHandler = new HttpHandler(httpRequest);
                            }
                            if (networkStream != null)
                            {
                                networkStream.Close();
                                networkStream.Dispose();
                                networkStream = null;
                            }
                            if (tcpClient != null)
                            {
                                tcpClient.Close();
                                tcpClient.Dispose();
                                tcpClient = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Write("处理请求出错", ex);
                            if (httpHandler == null)
                            {
                                httpHandler = new HttpHandler();
                                if (httpHandler.httpResponse == null)
                                {
                                    httpHandler.httpResponse = new HttpResponse();
                                    httpHandler.httpResponse.ResponseStream = tcpClient.GetStream();
                                    httpHandler.httpResponse.ContentType = "text/plain";
                                    httpHandler.httpResponse.ResponseBody = "处理您的请求出错了，" + ex.Message + "," + ex.Source + "," + ex.StackTrace;
                                    httpHandler.httpResponse.StatusCode = "500";
                                    httpHandler.httpResponse.StatusMessage = "Error";
                                    if (httpHandler.httpResponse.ResponseHeaders == null)
                                    {
                                        httpHandler.httpResponse.ResponseHeaders["Server"] = "softsunlight_webserver";
                                        httpHandler.httpResponse.ResponseHeaders["Date"] = DateTime.Now.ToLongTimeString();
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (httpHandler != null)
                            {
                                httpHandler.ProcessRequest();
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Write("启动web服务出错", ex);
                Thread.Sleep(10000);
                StartWebServer();
            }
        }

        /// <summary>
        /// 构造Http请求类
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="requestContent"></param>
        /// <returns></returns>
        private static HttpRequest BuildRequest(TcpClient tcpClient, string requestContent)
        {
            HttpRequest httpRequest = new HttpRequest();
            if (tcpClient.Connected)
            {
                httpRequest.RequestStream = tcpClient.GetStream();
            }
            if (string.IsNullOrEmpty(requestContent))
            {
                throw new Exception("请求内容为空");
            }
            string[] requestLines = requestContent.Split("\r\n");
            int formContentIndex = 0;
            for (int i = 0; i < requestLines.Length; i++)
            {
                if (i == 0)
                {
                    string[] tempArr = requestLines[i].Split(" ");
                    httpRequest.Method = tempArr[0];
                    httpRequest.RequstUrl = tempArr[1];
                    httpRequest.Version = tempArr[2];
                    string filePath = string.Empty;
                    string dir = AppDomain.CurrentDomain.BaseDirectory;
                }
                else if (formContentIndex > 0 && i >= formContentIndex)
                {
                    //构造表单域

                }
                else
                {
                    //请求头
                    //遇到空行，则下一行是表单域
                    if (string.IsNullOrEmpty(requestLines[i]))
                    {
                        formContentIndex = i + 1;
                        continue;
                    }
                    if (httpRequest.RequestHeaders == null)
                    {
                        httpRequest.RequestHeaders = new System.Collections.Generic.Dictionary<string, object>();
                    }
                    string[] tempArr = requestLines[i].Split(":");
                    if (tempArr.Length >= 2)
                    {
                        if (tempArr[0].Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase))
                        {
                            int length = 0;
                            try
                            {
                                length = Convert.ToInt32(tempArr[1].Trim());
                            }
                            catch (Exception ex)
                            {

                            }
                            httpRequest.RequestHeaders[tempArr[0].Trim()] = length;
                        }
                        else
                        {
                            httpRequest.RequestHeaders[tempArr[0].Trim()] = tempArr[1].Trim();
                        }
                    }
                    else
                    {
                        httpRequest.RequestHeaders[tempArr[0].Trim()] = "";
                    }
                }
            }
            return httpRequest;
        }

    }
}
