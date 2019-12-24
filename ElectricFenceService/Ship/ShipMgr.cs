using ElectricFenceService.Ship;
using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService
{
    public class ShipMgr
    {
        public readonly static ShipMgr Instance = new ShipMgr();
        List<ShipInfo> Ships = new List<ShipInfo>();
        public int ValidSecond { get; set; } = 180;

        ShipMgr()
        {
            new Thread(run) { IsBackground = true }.Start();
        }

        private void run()
        {
            while (true)
            {
                Thread.Sleep(1000);
                ///找到第一个未超时的船舶，之后将其前面的所有船舶记录清除
                lock (Ships)
                {
                    int index = Ships.FindIndex(_ => _.UpdateTime.AddSeconds(ValidSecond) > DateTime.Now);
                    if (index > 0)
                        Ships.RemoveRange(0, index);
                }
            }
        }
        #region 船舶数据接收与解析
        MuxReceiver _receiver;
        public void Start(string host, int port)
        {
            if (_receiver != null)
                _receiver.Dispose();
            _receiver = new MuxReceiver(host, port);
            _receiver.DynamicEvent += onDynamic;
        }

        void onDynamic(ShipInfo target)
        {
            Set(target);
        }
        #endregion 船舶数据接收与解析

        public void Set(ShipInfo info)
        {
            lock (Ships)
            {
                //removeFromMMSI(info.MMSI);
                if (Ships.Count > 0 && info.UpdateTime < Ships.Last().UpdateTime.AddMilliseconds(1))
                    info.UpdateTime = Ships.Last().UpdateTime.AddMilliseconds(1);
                Ships.Add(info);
            }
            ShipHistoryMgr.Instance.Update(info);
            Fence.ShipFenceMgr.Instance.Update(info);
        }

        public ShipInfo[] Get(string[] ids, DateTime start, DateTime end)
        {
            lock (Ships)
            {
                if (ids == null || ids.Length == 0)
                    return Ships.Where(_ => _.UpdateTime > start && _.UpdateTime <= end).ToArray();
                else
                {
                    List<ShipInfo> ships = new List<ShipInfo>();
                    foreach (var id in ids)
                    {
                        var array = ShipHistoryMgr.Instance.GetHistory(id, start, end);
                        if (array != null && array.Length > 0)
                            ships.AddRange(array);
                    }
                    return ships.ToArray();
                }
            }
        }

        private void removeFromMMSI(int mmsi)
        {
            var removeds = Ships.Where(_ => _.MMSI == mmsi);
            if (removeds.Count() > 0)
            {
                foreach (var r in removeds)
                    Ships.Remove(r);
            }
        }
    }
}
