using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class GateTrackInfo
    {
        public string RegionId { get; set; }    //区域对应的ID
        public bool IsInner { get; set; }   //区域是否为内部区域
        public string GateId { get; set; }//
        public ShipInfo Ship { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;//创建时间，即进入时间
        public DateTime LastestTime { get; set; }//最后一次船舶数据刷新时间
        public DateTime TrackTime { get; set; }//最后一次跟踪时间
        public int Priority { get; private set; }

        public bool IsTimeout { get { return LastestTime.AddSeconds(180) < DateTime.Now; } }
        public GateTrackInfo(string regionId, bool isInner, string gateId, ShipInfo ship)
        {
            RegionId = regionId;
            IsInner = isInner;
            GateId = gateId;
            Ship = ship;
            CreateTime = DateTime.Now;
            LastestTime = DateTime.Now;
            TrackTime = DateTime.Now;
            updatePriority();
        }

        public void Update(ShipInfo ship, string regionId, bool isInner)
        {
            Ship = ship;
            RegionId = regionId;
            IsInner = isInner;
            LastestTime = DateTime.Now;
            updatePriority();
        }

        private void updatePriority()
        {
            Priority = GetPriority(Ship, IsInner);
        }

        public static int GetPriority(ShipInfo ship, bool isInner)
        {
            int priority = 0;
            if (isInner)//内部优先级最高
                priority += 0x4000000;
            int level = ShieldData.GetLevel(ship.ShipCargoType);
            priority += (level << 16);
            priority += (int)ship.Length;
            return priority;
        }
    }
}
