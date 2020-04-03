using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
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
        string _path;
        OnlineMgr()
        {
            _path = ConfigData.OnlineFileName;
            load();
        }

        void load()
        {
            try
            {
                FileInfo fi = new FileInfo(_path);
                if (fi.Exists)
                {
                    string info = File.ReadAllText(_path);
                    IsoDateTimeConverter timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
                    _onlines = JsonConvert.DeserializeObject<Dictionary<string, OnlineInfo>>(info,timeConverter);
                    Common.Log.Logger.Default.Trace("Loaded Onlines，\r\n"+ JsonConvert.SerializeObject(_onlines, Formatting.Indented,timeConverter));
                    removeTimeouts();
                }
            }
            catch (Exception ex)
            {
                Common.Log.Logger.Default.Error("获取本地登录记录失败，", ex);
            }
        }

        void removeTimeouts()
        {
            var timeouts = _onlines.Where(_ => _.Value.UpdatedTime.AddDays(7) < DateTime.Now).ToArray();
            if (timeouts.Length > 0)
            {
                foreach (var to in timeouts)
                    _onlines.Remove(to.Key);
                Common.Log.Logger.Default.Trace("删除过期登录数量：" + timeouts.Length);
            }
        }

        void save()
        {
            try
            {
                removeTimeouts();
                    IsoDateTimeConverter timeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };
                File.WriteAllText(_path, JsonConvert.SerializeObject(_onlines, Formatting.Indented,timeConverter));
                Common.Log.Logger.Default.Trace("Save Onlines，\r\n"+ JsonConvert.SerializeObject(_onlines, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Common.Log.Logger.Default.Error("获取本地登录记录失败，", ex);
            }
        }

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
                save();
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

        public void Logout(string handle)
        {
            lock (_onlines)
            {
                if (!string.IsNullOrEmpty(handle))
                {
                    if (_onlines.Remove(handle))
                        save();
                }
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
            public string UserName { get; set; }
            public int Level { get; set; }
            public DateTime UpdatedTime { get; set; }
            public OnlineInfo()
            {
            }
            public OnlineInfo(string user, int level):this(user,level,DateTime.Now)
            {
            }

            public OnlineInfo(string user, int level, DateTime time)
            {
                UserName = user;
                Level = level;
                UpdatedTime = time;
            }

            public void Update()
            {
                UpdatedTime = DateTime.Now;
            }
        }
    }
}
