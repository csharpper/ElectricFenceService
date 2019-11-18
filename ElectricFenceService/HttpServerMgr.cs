using ElectricFenceService.Http;
using ElectricFenceService.Shield;
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
                        var remoteendport = hRequest.RemoteEndPoint;
                        var key = hRequest.Headers.AllKeys.FirstOrDefault(_=>_ == "key");
                        HttpRequestInfo reqInfo = HttpRequestInfo.Create(hRequest.RawUrl);
                        if(reqInfo == null)
                            writer.WriteLine("error,请输入有效的参数.");
                        else
                        {
                            switch (reqInfo.Sort)
                            {
                                case "searchall":
                                    writer.WriteLine(FenceMgr.Instance.ToJson());
                                    break;
                                case "setregion":
                                    FenceMgr.Instance.Set<FenceRegionsInfo>(hRequest.InputStream);
                                    break;
                                case "setgate":
                                    FenceMgr.Instance.Set<GateInfo>(hRequest.InputStream);
                                    break;
                                case "setbridge":
                                    FenceMgr.Instance.Set<Bridge>(hRequest.InputStream);
                                    break;
                                case "ship":
                                    writeShipInfo(writer, reqInfo.Source);
                                    break;
                                case "shield":
                                    writeShields(writer, reqInfo.Source);
                                    break;
                                case "deletegate":
                                case "deleteregion":
                                case "deletebridge":
                                    writeDeleteFence(writer, reqInfo);
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
                        writer.WriteLine("error,request error." + ex.Message);
                    }
                }
            }
            _thread = null;
            Stop();
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

        private void writeDeleteFence(StreamWriter writer, HttpRequestInfo req)
        {
            if (req.Source == null || req.Source.Length == 0)
                throw new InvalidCastException("未找到需要删除的字段.");
            List<string> gates = null;
            List<string> regions = null;
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
            }
            foreach (var source in req.Source)
            {
                    if (gates != null && source.Name.Equals("gateid"))
                        gates.Add(source.Setting);
                    if (regions != null && source.Name.Equals("regionid"))
                        regions.Add(source.Setting);
            }
            FenceMgr.Instance.Delete(gates, regions);
            writer.WriteLine("seccess，删除已完成。");
        }

        #endregion

        #region 电子围栏屏蔽船舶
        private void writeShields(StreamWriter writer, SortSource[] sources)
        {
            if (sources == null || sources.Length == 0)
            {
                writer.WriteLine(ShieldMgr.Instance.ToJson());
            }
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
                writer.WriteLine("seccess,配置完成。");
            }
        }

        #endregion
    }
}
