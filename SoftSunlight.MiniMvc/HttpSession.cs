using System;
using System.Collections.Generic;
using System.Text;

namespace SoftSunlight.MiniMvc
{
    /// <summary>
    /// Http会话
    /// </summary>
    public class HttpSession
    {

        private HttpRequest httpRequest;

        private HttpResponse httpResponse;

        private string userKey;

        private string userPrefix = "softsunlight_session_id";

        public HttpSession(HttpRequest httpRequest, HttpResponse httpResponse)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException();
            }
            if (httpResponse == null)
            {
                throw new ArgumentNullException();
            }
            this.httpRequest = httpRequest;
            this.httpResponse = httpResponse;
            if (this.httpRequest.Cookies != null)
            {
                foreach (var cookie in this.httpRequest.Cookies)
                {
                    if (cookie.Name.Equals(userPrefix))
                    {
                        userKey = userPrefix + "_" + cookie.Value;
                        break;
                    }
                }
            }
        }

        public object this[string name]
        {
            get
            {
                return SessionManager.Get(userKey, name);
            }

            set
            {
                if (string.IsNullOrEmpty(userKey) || SessionManager.ExistUser(userKey))
                {
                    string guid = Guid.NewGuid().ToString();
                    userKey = userPrefix + "_" + guid;
                    if (httpResponse.Cookies == null)
                    {
                        httpResponse.Cookies = new List<HttpCookie>();
                    }
                    httpResponse.Cookies.Add(new HttpCookie(userPrefix, guid));
                }
                SessionManager.Add(userKey, name, value);
            }
        }

    }
}
