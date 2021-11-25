using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ThinkInAspNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
            tcpListener.Start();
            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                Console.WriteLine(tcpClient.ToString());
                Task.Run(() =>
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
                        string[] requestLines = content.Split("\r\n");
                        StringBuilder responseBuilder = new StringBuilder();
                        for (int i = 0; i < requestLines.Length; i++)
                        {
                            if (i == 0)
                            {
                                string[] tempArr = requestLines[i].Split(" ");
                                string method = tempArr[0];
                                string requestUrl = tempArr[1];
                                string httpVersion = tempArr[2];
                                string filePath = string.Empty;
                                string dir = AppDomain.CurrentDomain.BaseDirectory;
                                if (requestUrl == "/")
                                {
                                    filePath = Path.Combine(dir, "index.html");
                                }
                                else
                                {
                                    filePath = Path.Combine(dir, requestUrl.Remove(0, 1));
                                }
                                if (!File.Exists(filePath))
                                {
                                    responseBuilder.Append(method).Append(" 404 aa\r\n");
                                    responseBuilder.Append("Server:softsunlight_webserver\r\n");
                                    if (filePath.LastIndexOf("html") >= 0)
                                    {
                                        responseBuilder.Append("Content-Type:text/html\r\n");
                                    }
                                    else if (filePath.LastIndexOf("css") >= 0)
                                    {
                                        responseBuilder.Append("Content-Type:text/css\r\n");
                                    }
                                    else
                                    {
                                        responseBuilder.Append("Content-Type:text/html\r\n");
                                    }
                                }
                                else
                                {
                                    responseBuilder.Append(httpVersion).Append(" 200 Success\r\n");
                                    responseBuilder.Append("Server:softsunlight_webserver\r\n");
                                    if (filePath.LastIndexOf("html") >= 0)
                                    {
                                        responseBuilder.Append("Content-Type:text/html\r\n");
                                    }
                                    else if (filePath.LastIndexOf("css") >= 0)
                                    {
                                        responseBuilder.Append("Content-Type:text/css\r\n");
                                    }
                                    else
                                    {
                                        responseBuilder.Append("Content-Type:text/html\r\n");
                                    }
                                    responseBuilder.Append("\r\n");
                                    responseBuilder.Append(File.ReadAllText(filePath));
                                }
                            }
                        }
                        if (responseBuilder.Length > 0)
                        {
                            networkStream.Write(Encoding.UTF8.GetBytes(responseBuilder.ToString()));
                        }
                        if (memoryStream != null)
                        {
                            memoryStream.Close();
                            memoryStream.Dispose();
                            memoryStream = null;
                        }
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine(content);
                        }
                    }
                    //else
                    {
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
    }
}
