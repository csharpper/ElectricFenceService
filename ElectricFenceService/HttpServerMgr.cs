using ElectricFenceService.Http;
using ElectricFenceService.Shield;
using ElectricFenceService.User;
using Fence.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ElectricFenceService
{
    public class HttpServerMgr
    {
        public static readonly HttpServerMgr Instance = new HttpServerMgr();
        int _port;
        Thread _thread;
        HttpListener _httpListener;
        ManualResetEvent _disposeEvent = new ManualResetEvent(false);
        object _obj = new object();
        public void Start(int port)
        {
            if (_port == port)
                return;
            Stop();
            lock (_obj)
                start(port);
        }

        private void start(int port)
        {
            Common.Log.Logger.Default.Trace("---------------start http server.-------------");
            if (port > 0 && port <= 65535)
            {
                _port = port;
                _httpListener = new HttpListener();
                _httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                _httpListener.Prefixes.Add($"http://+:{port}/");
                _httpListener.Start();
                Common.Log.Logger.Default.Trace($"可通过http访问本机的{port}端口验证.");
                _disposeEvent.Reset();
                _thread = new Thread(new ThreadStart(delegate
                {
                    try { loop(); }
                    catch { Stop(); }
                }))
                { IsBackground = true };
                _thread.Start();
            }
            else
                throw new InvalidCastException("未配置有效的端口");
        }

        private void Stop()
        {
            lock (_obj)
            {
                _thread?.Join(15000);
                _port = 0;
                _disposeEvent.Set();
                try
                {
                    _httpListener?.Stop();
                    Common.Log.Logger.Default.Trace("---------------stop http server.-------------");
                }
                catch { }
                _thread = null;
            }
        }

        void loop()
        {
            int port = _port;
            while (!_disposeEvent.WaitOne(0) && port == _port)
            {
                HttpListenerContext context = _httpListener.GetContext();
                HttpListenerRequest hRequest = context.Request;
                HttpListenerResponse hResponse = context.Response;
                using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
                {
                    try
                    {
                        var endport = hRequest.RemoteEndPoint;
                        string key = readHeader(hRequest.Headers,"handle");
                        var onlineInfo = key != null ? OnlineMgr.Instance.GetOnlineInfo(key): null;
                        HttpRequestInfo reqInfo = HttpRequestInfo.Create(hRequest.RawUrl);
                        var headers = hRequest.Headers;
                        if (reqInfo == null)
                            writer.WriteLine("error,请输入有效的参数.");
                        else
                        {
                            switch (reqInfo.Sort)
                            {
                                #region 用户登录、退出及用户操作
                                case "login":
                                    if (headers.AllKeys.Any(_ => _ == "user") && headers.AllKeys.Any(_ => _ == "pass"))
                                        key = OnlineMgr.Instance.Login(headers["user"], headers["pass"]);
                                    else
                                        throw new InvalidCastException("未找到登录的用户名或密码。");
                                    if (string.IsNullOrEmpty(key))
                                        throw new InvalidCastException("用户名或密码错误。");
                                    onlineInfo = OnlineMgr.Instance.GetOnlineInfo(key);
                                    UserInfo userInfo = UserMgr.Instance.Get(onlineInfo.UserName);
                                    writer.WriteLine($"{key},{userInfo.Longitude},{userInfo.Latitude},{userInfo.Scale},{onlineInfo.Level}");
                                    break;
                                case "logout":
                                    OnlineMgr.Instance.Logout(key);
                                    onlineInfo = null;
                                    writer.WriteLine("seccess");
                                    break;
                                case "user":
                                    writer.WriteLine(JsonConvert.SerializeObject(onlineInfo, Formatting.Indented));
                                    break;
                                case "users":
                                    checkRead(onlineInfo);
                                    writer.WriteLine(UserMgr.Instance.ToJsonSafe());
                                    break;
                                case "changepass":
                                    if (onlineInfo == null)
                                        throw new InvalidCastException("当前未登录或登录已过期,请退出重新登录.");
                                    if (!headers.AllKeys.Any(_ => _ == "pass"))
                                        throw new InvalidCastException("未找到新密码，修改失败.");
                                    UserMgr.Instance.ChangePass(onlineInfo.UserName, headers["pass"]);
                                    writer.WriteLine("seccess");
                                    break;
                                case "adduser":
                                    checkWrite(onlineInfo);
                                    addUser(headers);
                                    writer.WriteLine("seccess");
                                    break;
                                case "updateuser":
                                    checkWrite(onlineInfo);
                                    updateUser(headers);
                                    writer.WriteLine("seccess");
                                    break;
                                case "deleteuser":
                                    checkWrite(onlineInfo);
                                    int count = deleteUser(reqInfo.Source);
                                    writer.WriteLine($"seccess,成功删除 {count} 个用户。");
                                    break;
                                #endregion 用户登录、退出及用户操作
                                #region 围栏信息查询及增删改操作
                                case "searchall":
                                    writer.WriteLine(FenceMgr.Instance.ToJsonFromUser(onlineInfo));
                                    break;
                                case "gate":
                                    checkRead(onlineInfo);
                                    writer.WriteLine(FenceMgr.Instance.Read(getIds(reqInfo), FenceNum.Gate));
                                    break;
                                case "region":
                                    checkRead(onlineInfo);
                                    writer.WriteLine(FenceMgr.Instance.Read(getIds(reqInfo),FenceNum.Region));
                                    break;
                                case "gatebridge":
                                    checkRead(onlineInfo);
                                    writer.WriteLine(FenceMgr.Instance.Read(getIds(reqInfo), FenceNum.BridgeGate));
                                    break;
                                case "regionbridge":
                                    checkRead(onlineInfo);
                                    writer.WriteLine(FenceMgr.Instance.Read(getIds(reqInfo), FenceNum.BridgeRegion));
                                    break;
                                case "setregion":
                                    checkWrite(onlineInfo);
                                    FenceMgr.Instance.Set<FenceRegionsInfo>(hRequest.InputStream);
                                    break;
                                case "setgate":
                                    checkWrite(onlineInfo);
                                    FenceMgr.Instance.Set<GateInfo>(hRequest.InputStream);
                                    break;
                                case "deletegate":
                                case "deleteregion":
                                    checkWrite(onlineInfo);
                                    settingFence(reqInfo);
                                    writer.WriteLine("seccess");
                                    break;
                                case "setbridge":
                                case "deletebridge":
                                    checkRead(onlineInfo);
                                    settingFence(reqInfo);
                                    writer.WriteLine("seccess");
                                    break;
                                #endregion 围栏信息查询及增删改操作
                                case "ship":
                                    writeShipInfo(writer, reqInfo.Source);
                                    break;
                                case "shield":
                                    checkWrite(onlineInfo);
                                    writeShields(writer, reqInfo.Source);
                                    break;
                                default:
                                    writer.WriteLine("error,不支持的消息字段.");
                                    break;
                            }

                        }
                    }
                    catch (InvalidCastException ex)
                    {
                        writer.WriteLine("error," + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("error,解析错误." + ex.Message);
                    }
                }
            }
            _thread = null;
            Stop();
        }

        private string[] getIds(HttpRequestInfo reqInfo)
        {
            return reqInfo.Source.Where(_ => _.Name == "id").Select(_ => _.Setting).ToArray();
        }

        string readHeader(System.Collections.Specialized.NameValueCollection headers, string key)
        {
            if (headers.AllKeys.Any(_ => _ == key))
                return headers[key];
            return null;
        }

        private int deleteUser(SortSource[] source)
        {
            var users = source.Where(_ => _.Name.Equals("user")).Select(_ => _.Setting);
            return UserMgr.Instance.Delete(users);
        }

        private void addUser(System.Collections.Specialized.NameValueCollection headers)
        {
            UserInfo user = getUser(headers);
            if (UserMgr.Instance.IsExist(user.UserName))
                throw new InvalidCastException("创建失败，用户已存在。");
            UserMgr.Instance.Set(user);
        }

        void updateUser(System.Collections.Specialized.NameValueCollection headers)
        {
            UserInfo newUser = getUser(headers);
            UserInfo oldUser = UserMgr.Instance.Get(newUser.UserName);
            if (oldUser == null)
                throw new InvalidCastException("更新失败，用户不存在。");
            
            if(!string.IsNullOrEmpty(newUser.Password) || newUser.Password == "null")
                oldUser.Password = newUser.Password;
            if(newUser.Longitude > -180 && newUser.Longitude <= 180)
                oldUser.Longitude = newUser.Longitude;
            if (newUser.Latitude > -85 && newUser.Latitude < 85)
                oldUser.Latitude = newUser.Latitude;
            if (newUser.Scale > 1)
                oldUser.Scale = newUser.Scale;
            UserMgr.Instance.Set(oldUser);
        }

        UserInfo getUser(System.Collections.Specialized.NameValueCollection headers)
        {
            string user = readHeader(headers, "user");
            string pass = readHeader(headers, "pass");
            string strLon = readHeader(headers, "lon");
            string strLat = readHeader(headers, "lat"); 
            string strScale = readHeader(headers, "scale");
            if (string.IsNullOrEmpty(user))
                throw new InvalidCastException("用户名无效。");
            double lon = 181;// 122.1458;
            if (!string.IsNullOrEmpty(strLon))
                double.TryParse(strLon, out lon);
            double lat = 91;// 30.0502;
            if (!string.IsNullOrEmpty(strLat))
                double.TryParse(strLat, out lat);
            int scale = 0;// 235190;
            if (!string.IsNullOrEmpty(strScale))
                int.TryParse(strScale, out scale);
            int level = 3;
            //if (level < 1 || level > 3)
            //    throw new InvalidCastException("用户权限配置错误。");
            return new UserInfo()
            {
                UserName = user,
                Password = pass,
                Longitude = lon,
                Latitude = lat,
                Scale = scale,
                Level = level
            };
        }

        bool isCanRead(OnlineMgr.OnlineInfo info)
        {
            return info != null && (info.UserName == "system" || info.UserName == "admin");
        }

        void checkRead(OnlineMgr.OnlineInfo info)
        {
            if (!isCanRead(info))
                throw new InvalidCastException("无操作权限。");
        }

        bool isCanWrite(OnlineMgr.OnlineInfo info)
        {
            return info != null && info.UserName == "system";
        }

        void checkWrite(OnlineMgr.OnlineInfo info)
        {
            if(!isCanWrite(info))
                throw new InvalidCastException("无操作权限。");
        }

        #region 查询船舶相关信息

        private void writeShipInfo(StreamWriter writer, SortSource[] reqs)
        {
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MaxValue;
            List<string> ids = new List<string>();
            if (reqs != null && reqs.Length > 0)
            {
                foreach (var data in reqs)
                {
                    switch (data.Name)
                    {
                        case "mmsi":
                            ids.Add(data.Setting);
                            break;
                        case "start":
                            start = DateTime.ParseExact(data.Setting, "yyyyMMddHHmmssfff", CultureInfo.CurrentCulture, DateTimeStyles.None);
                            break;
                        case "end":
                            end = DateTime.ParseExact(data.Setting, "yyyyMMddHHmmssfff", CultureInfo.CurrentCulture, DateTimeStyles.None);
                            break;
                    }
                }
            }
            var historys = ShipMgr.Instance.Get(ids.ToArray(),start, end);
            if (historys.Length == 0)
                writer.WriteLine("null");
            else
            {
                writer.WriteLine($"{historys.Min(_ => _.UpdateTime).ToString("yyyyMMddHHmmssfff")}-{historys.Max(_ => _.UpdateTime).AddSeconds(1).ToString("yyyyMMddHHmmssfff")}");
                for (int i = 0; i < historys.Length; i++)
                    writer.WriteLine(historys[i].ToFormat());
            }
        }
        #endregion

        #region 删除电子围栏信息

        private void settingFence(HttpRequestInfo req)
        {
            if (req.Source == null || req.Source.Length == 0)
                throw new InvalidCastException("未找到需要删除的字段.");
            List<string> gates = null;
            List<string> regions = null;
            bool isDelete = true;
            switch (req.Sort)
            {
                case "deletegate":
                    gates = new List<string>();
                    break;
                case "deleteregion":
                    regions = new List<string>();
                    break;
                case "deletebridge":
                    gates = new List<string>();
                    regions = new List<string>();
                    break;
                case "setbridge":
                    isDelete = false;
                    gates = new List<string>();
                    regions = new List<string>();
                    break;
            }
            foreach (var source in req.Source)
            {
                if (gates != null && source.Name.Equals("gateid"))
                    gates.Add(source.Setting);
                if (regions != null && source.Name.Equals("regionid"))
                    regions.Add(source.Setting);
            }
            if(isDelete)
                FenceMgr.Instance.Delete(gates, regions);
            else
                FenceMgr.Instance.AddBridge(gates, regions);
        }

        #endregion

        #region 电子围栏屏蔽船舶设置
        private void writeShields(StreamWriter writer, SortSource[] sources)
        {
            if (sources == null || sources.Length == 0)
                writer.WriteLine(ShieldMgr.Instance.ToJson());
            else
            {
                bool isAdd = false;
                bool isDelete = false;
                List<int> types = new List<int>();
                List<int> mmsis = new List<int>();
                List<string> names = new List<string>();

                foreach (var source in sources)
                {
                    switch (source.Name)
                    {
                        case "action":
                            if (source.Setting == "add")
                                isAdd = true;
                            else if (source.Setting == "delete")
                                isDelete = true;
                            break;
                        case "type":
                            int type = 0;
                            if (int.TryParse(source.Setting, out type))
                                types.Add(type);
                            break;
                        case "mmsi":
                            int mmsi = 0;
                            if (int.TryParse(source.Setting, out mmsi) && mmsi > 0)
                                mmsis.Add(mmsi);
                            break;
                    }
                }
                if (isAdd)
                {
                    if (mmsis.Count > 0)
                        ShieldMgr.Instance.SetFromMMSI(mmsis);
                    if (types.Count > 0)
                        ShieldMgr.Instance.SetFromType(types);
                }
                else if (isDelete)
                {
                    if (mmsis.Count > 0)
                        ShieldMgr.Instance.RemoveFromMMSI(mmsis);
                    if (types.Count > 0)
                        ShieldMgr.Instance.RemoveFromTypes(types);
                }
                else
                    throw new InvalidCastException("未知的字段。");
                writer.WriteLine("seccess");
            }
        }
        #endregion
    }
}
