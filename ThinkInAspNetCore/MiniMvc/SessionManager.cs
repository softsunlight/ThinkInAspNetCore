using System;
using System.Collections.Generic;
using System.Text;

namespace ThinkInAspNetCore.MiniMvc
{
    /// <summary>
    /// 管理服务器中的session
    /// </summary>
    internal class SessionManager
    {
        /// <summary>
        /// 过期时间
        /// </summary>
        private static Dictionary<string, DateTime> userId2Expire;

        private static Dictionary<string, Dictionary<string, object>> userDataDic;

        private static int minutes = 20;

        public static void Add(string userId, string name, object value)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(name))
            {
                return;
            }
            if (userDataDic == null)
            {
                userDataDic = new Dictionary<string, Dictionary<string, object>>();
            }
            if (!userDataDic.ContainsKey(userId))
            {
                userDataDic[userId] = new Dictionary<string, object>();
                if (userId2Expire == null)
                {
                    userId2Expire = new Dictionary<string, DateTime>();
                }
                userId2Expire[userId] = DateTime.Now.AddMinutes(minutes);
            }
            userDataDic[userId][name] = value;
        }

        public static void Remove(string userId, string name)
        {
            if (userDataDic == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(userId) && userDataDic.ContainsKey(userId) && userDataDic[userId].ContainsKey(name))
            {
                userDataDic[userId].Remove(name);
            }
        }

        public static void Remove(string userId)
        {
            if (userDataDic == null)
            {
                return;
            }
            if (!string.IsNullOrEmpty(userId) && userDataDic.ContainsKey(userId))
            {
                userDataDic.Remove(userId);
            }
        }

        public static object Get(string userId, string name)
        {
            if (userDataDic == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(userId) || !userDataDic.ContainsKey(userId))
            {
                return null;
            }
            if (userId2Expire != null && userId2Expire.ContainsKey(userId) && userId2Expire[userId] > DateTime.Now)
            {
                userId2Expire[userId] = DateTime.Now.AddMinutes(minutes);
                return userDataDic[userId][name];
            }
            //过期删除
            userDataDic.Remove(userId);
            userId2Expire.Remove(userId);
            return null;
        }

        public static bool ExistUser(string userId)
        {
            if (userDataDic == null)
            {
                return false;
            }
            return userDataDic.ContainsKey(userId);
        }

    }
}
