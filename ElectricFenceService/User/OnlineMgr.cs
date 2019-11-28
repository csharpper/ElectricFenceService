using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.User
{
    public class OnlineMgr
    {
        public readonly static OnlineMgr Instance = new OnlineMgr();
        Dictionary<string, OnlineInfo> _onlines = new Dictionary<string, OnlineInfo>();
        public bool Level { get; set; } = false;
        /// <summary>
        /// 系统登录，成功返回Handle字符串，失败返回null
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public string Login(string user, string pass)
        {
            lock (_onlines)
            {
                int level = UserMgr.Instance.Login(user, pass);
                if (level < 0)
                {
                    Common.Log.Logger.Default.Error($"登录失败, {user},{pass}");
                    return null;
                }
                string iep = Guid.NewGuid().ToString();
                lock (_onlines)
                    _onlines[iep] = new OnlineInfo(user, level);
                Common.Log.Logger.Default.Trace($"{user} 登录。");
                return iep;
            }
        }
        string iptostring(IPEndPoint endpoint)
        {
            if(!Level)
                return endpoint.Address.ToString();//IP满足条件
            else
                return endpoint.ToString();//IP和端口同时满足条件
        }

        public bool Logout(string handle)
        {
            lock (_onlines)
            {
                if(!string.IsNullOrEmpty(handle))
                    return _onlines.Remove(handle);
                return false;
            }
        }

        public OnlineInfo GetOnlineInfo(string handle)
        {
            lock (_onlines)
            {
                if (_onlines.ContainsKey(handle))
                {
                    _onlines[handle].Update();
                    return _onlines[handle];
                }
                return null;
            }
        }

        public class OnlineInfo: UserBase
        {
            public string UserName { get; private set; }
            public int Level { get; private set; }
            public DateTime UpdatedTime { get; private set; }
            public OnlineInfo(string user, int level)
            {
                UserName = user;
                Level = level;
                UpdatedTime = DateTime.Now;
            }

            public void Update()
            {
                UpdatedTime = DateTime.Now;
            }
        }
    }
}
