using System;
using System.Collections.Generic;
using System.Text;

namespace SoftSunlight.MiniMvc
{
    /// <summary>
    /// Cookie
    /// </summary>
    public class HttpCookie
    {
        public HttpCookie()
        {

        }

        public HttpCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public HttpCookie(string name, string value, string path) : this(name, value)
        {
            Path = path;
        }

        /// <summary>
        /// Cookie 键名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Cookie 键值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime Expires { get; set; }
        /// <summary>
        /// Max-Age 在 cookie 失效之前需要经过的秒数。秒数为 0 或 -1 将会使 cookie 直接过期 假如二者 （指 Expires 和Max-Age） 均存在，那么 Max-Age 优先级更高
        /// </summary>
        public int MaxAge { get; set; }
        /// <summary>
        /// cookie可以送达的主机名
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 指定一个 URL 路径，这个路径必须出现在要请求的资源的路径中才可以发送 Cookie 首部
        /// </summary>
        public string Path { get; set; } = "/";
        /// <summary>
        /// 一个带有安全属性的 cookie 只有在请求使用SSL和HTTPS协议的时候才会被发送到服务器
        /// </summary>
        public bool Secure { get; set; }
        /// <summary>
        /// 设置了 HttpOnly 属性的 cookie 不能使用 JavaScript 经由  Document.cookie 属性、XMLHttpRequest 和  Request APIs 进行访问，以防范跨站脚本攻击（XSS (en-US)）
        /// </summary>
        public bool HttpOnly { get; set; }

    }
}
