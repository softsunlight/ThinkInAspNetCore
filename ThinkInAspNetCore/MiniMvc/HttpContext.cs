using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// http请求上下文环境
    /// </summary>
    public class HttpContext
    {
        /// <summary>
        /// Socket
        /// </summary>
        public TcpClient TcpClient { get; set; }
        /// <summary>
        /// 原始请求数据
        /// </summary>
        public byte[] OriginalRequestData { get; set; }
        /// <summary>
        /// Http请求类
        /// </summary>
        public HttpRequest Request { get; set; }
        /// <summary>
        /// Http响应类
        /// </summary>
        public HttpResponse Response { get; set; }
    }
}
