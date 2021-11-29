using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThinkInAspNetCore.MiniMvc;

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
                        NetworkStream networkStream = null;
                        HttpHandler httpHandler = null;
                        HttpRequest httpRequest = new HttpRequest();
                        try
                        {
                            networkStream = tcpClient.GetStream();
                            httpRequest.RequestStream = networkStream;
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
                                httpRequest = BuildRequest(tcpClient, content);
                                httpHandler = new HttpHandler(httpRequest);
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
                                    httpHandler.httpResponse.ContentType = "text/html";
                                    httpHandler.httpResponse.ResponseBody = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_500.html")).Replace("@error", "处理您的请求出错了，" + ex.Message + "," + ex.Source + "," + ex.StackTrace);
                                    httpHandler.httpResponse.StatusCode = "500";
                                    httpHandler.httpResponse.StatusMessage = "Error";
                                    if (httpHandler.httpResponse.ResponseHeaders == null)
                                    {
                                        httpHandler.httpResponse.ResponseHeaders = new Dictionary<string, object>();
                                    }
                                    httpHandler.httpResponse.ResponseHeaders["Server"] = "softsunlight_webserver";
                                    httpHandler.httpResponse.ResponseHeaders["Date"] = DateTime.Now.ToLongTimeString();
                                }
                            }
                        }
                        finally
                        {
                            if (httpHandler != null)
                            {
                                httpHandler.ProcessRequest();
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
            StringBuilder fileFieldBuilder = new StringBuilder();
            for (int i = 0; i < requestLines.Length; i++)
            {
                if (i == 0)
                {
                    string[] tempArr = requestLines[i].Split(" ");
                    httpRequest.Method = tempArr[0];
                    string requestUri = tempArr[1];
                    string[] urlAndQueryString = requestUri.Split("?");
                    if (urlAndQueryString.Length >= 2)
                    {
                        httpRequest.RequstUrl = urlAndQueryString[0];
                        string[] queryStrings = urlAndQueryString[1].Split("&");
                        foreach (var queryString in queryStrings)
                        {
                            string[] keyvalues = queryString.Split("=");
                            if (keyvalues.Length >= 2)
                            {
                                if (httpRequest.QueryString == null)
                                {
                                    httpRequest.QueryString = new Dictionary<string, string>();
                                }
                                httpRequest.QueryString[keyvalues[0].Trim()] = keyvalues[1].Trim();
                            }
                        }
                    }
                    else if (urlAndQueryString.Length >= 1)
                    {
                        httpRequest.RequstUrl = urlAndQueryString[0];
                    }
                    httpRequest.Version = tempArr[2];
                }
                else if (formContentIndex > 0 && i >= formContentIndex)
                {
                    //构造表单域
                    if (httpRequest.RequestHeaders.ContainsKey("Content-Type") && httpRequest.RequestHeaders["Content-Type"].ToString().Contains("multipart/form-data"))
                    {
                        if (string.IsNullOrEmpty(requestLines[i]))
                        {
                            fileFieldBuilder.Append(Environment.NewLine);
                        }
                        else
                        {
                            fileFieldBuilder.Append(requestLines[i]);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(requestLines[i]))
                        {
                            string[] formFields = requestLines[i].Split("&");
                            foreach (var field in formFields)
                            {
                                string[] keyvalues = field.Split("=");
                                if (keyvalues.Length >= 2)
                                {
                                    if (httpRequest.Form == null)
                                    {
                                        httpRequest.Form = new Dictionary<string, string>();
                                    }
                                    httpRequest.Form[keyvalues[0]] = keyvalues[1];
                                }
                            }
                        }
                    }
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
                        httpRequest.RequestHeaders = new Dictionary<string, object>();
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
                        else if (tempArr[0].Equals("Cookie", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string[] cookies = tempArr[1].Split(";");
                            foreach (var cookie in cookies)
                            {
                                string[] keyvalues = cookie.Split("=");
                                if (keyvalues.Length >= 2)
                                {
                                    if (httpRequest.Cookies == null)
                                    {
                                        httpRequest.Cookies = new List<HttpCookie>();
                                    }
                                    httpRequest.Cookies.Add(new HttpCookie(keyvalues[0].Trim(), keyvalues[1].Trim()));
                                }
                            }
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
            if (fileFieldBuilder.Length > 0)
            {
                //构造文件列表
                Regex boundaryRegex = new Regex(@"boundary=(?<boundary>\S+)");
                Match boundaryMatch = boundaryRegex.Match(httpRequest.RequestHeaders["Content-Type"].ToString());
                string boundary = string.Empty;
                if (boundaryMatch.Success)
                {
                    boundary = boundaryMatch.Groups["boundary"].Value;
                }
                string[] formDataArr = fileFieldBuilder.ToString().Split("--" + boundary, StringSplitOptions.RemoveEmptyEntries);
                Regex nameReg = new Regex(@"name=""(?<name>[^""]*)""");
                Regex filenameReg = new Regex(@"filename=""(?<filename>[^""]*)""");
                Regex contentTypeReg = new Regex(@"Content-Type:(?<contentType>\S*)");
                foreach (var formData in formDataArr)
                {
                    string[] formInfos = formData.Split("\r\n");
                    if (formInfos.Length >= 2)
                    {
                        string fileHeaderInfo = formInfos[0];
                        string value = formInfos[1];
                        Match nameMatch = nameReg.Match(fileHeaderInfo);
                        string name = string.Empty;
                        if (nameMatch.Success)
                        {
                            name = nameMatch.Groups["name"].Value;
                        }
                        Match filenameMatch = filenameReg.Match(fileHeaderInfo);
                        string fileName = string.Empty;
                        if (filenameMatch.Success)
                        {
                            fileName = filenameMatch.Groups["filename"].Value;
                        }
                        Match contentTypeMatch = contentTypeReg.Match(fileHeaderInfo);
                        string contentType = string.Empty;
                        if (contentTypeMatch.Success)
                        {
                            contentType = contentTypeMatch.Groups["contentType"].Value;
                        }
                        if (string.IsNullOrEmpty(fileName))
                        {
                            if (!string.IsNullOrEmpty(name))
                            {
                                if (httpRequest.Form == null)
                                {
                                    httpRequest.Form = new Dictionary<string, string>();
                                }
                                httpRequest.Form[name] = value;
                            }
                        }
                        else
                        {
                            if (httpRequest.Files == null)
                            {
                                httpRequest.Files = new List<HttpFile>();
                            }
                            httpRequest.Files.Add(new HttpFile()
                            {
                                Name = name,
                                FileName = fileName,
                                FileDatas = Encoding.Unicode.GetBytes(value),
                                ContentType = contentType
                            });
                        }

                    }
                }
            }
            return httpRequest;
        }

    }
}
