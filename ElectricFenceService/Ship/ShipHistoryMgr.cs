using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Ship
{
    public class ShipHistoryMgr
    {
        public readonly static ShipHistoryMgr Instance = new ShipHistoryMgr();
        Dictionary<string, Queue<ShipInfo>> Historys = new Dictionary<string, Queue<ShipInfo>>();
        /// <summary>
        /// 历史数据默认支持的最大存储长度
        /// </summary>
        public int HistorySeconds { get; set; } = 3600;
        ShipHistoryMgr()
        {
            new Thread(run) { IsBackground = true }.Start();
        }

        private void run()
        {
            while (true)
            {
                Thread.Sleep(30000);
                ///每三十秒刷新一次，清理一次过期历史船舶
                lock (Historys)
                {
                    var removeds = Historys.Where(_ => isTimeout(_.Value.LastOrDefault())).Select(_=>_.Key).ToArray();
                    if (removeds.Count()> 0)
                    {
                        foreach (var r in removeds)
                            Historys.Remove(r);
                        Common.Log.Logger.Default.Trace($"删除{removeds.Count()}条过期的船舶历史数据.");
                    }
                }
            }
        }

        public void Update(ShipInfo ship)
        {
            lock (Historys)
            {
                string id = ship.MMSI > 0 ? ship.MMSI.ToString() : ship.ID;
                if (Historys.ContainsKey(id))//如果存在，检查过期情况。移除第一个过期的历史数据
                {
                    while (Historys[id].Count > 0 && isTimeout(Historys[id].Peek()))
                        Historys[id].Dequeue();
                }
                else//如果不存在则新建
                    Historys[id] = new Queue<ShipInfo>();
                Historys[id].Enqueue(ship);
            }
        }

        public ShipInfo[] GetHistory(string id, DateTime start, DateTime end)
        {
            lock (Historys)
            {
                if (Historys.ContainsKey(id))
                    return Historys[id].Where(_ => _.UpdateTime >= start && _.UpdateTime <= end).ToArray();
                return null;
            }
        }

        bool isTimeout(ShipInfo ship)
        {
            return ship == null || ship.UpdateTime.AddSeconds(HistorySeconds) < DateTime.Now;
        }
    }
}
