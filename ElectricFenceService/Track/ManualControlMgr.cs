using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Track
{
    /// <summary>
    /// 手动控制管理，记录所有手动控制信息
    /// </summary>
    public class ManualControlMgr
    {
        const int TimeoutSecond = 30;
        public static ManualControlMgr Instance { get; } = new ManualControlMgr();
        public Dictionary<string,DateTime> History { get; set; }
        public void Update(string gateId)
        {
            lock(History)
                History[gateId] = DateTime.Now;
        }

        public bool IsManualControl(string gateId)
        {
            lock (History)
            {
                if (!History.ContainsKey(gateId))
                    return false;
                else
                    return History[gateId].AddSeconds(TimeoutSecond) > DateTime.Now;
            }
        }

    }
}
