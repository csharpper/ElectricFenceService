using ElectricFenceService.Util;
using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Track
{
    public class ShipTrackConfig
    {
        public ShipInfo Ship { get; private set; }
        //public string RegionId { get; set; }//船舶所在区域ID
        //public DateTime UpdatedTime { get; private set; }
        public DateTime OutstandTime { get; private set; } = DateTime.MinValue;//报警时间
        public bool IsOutstanded { get; set; } = false;//是否已完成报警
        public DateTime MoveOutTime { get; private set; } = DateTime.MinValue;
        public DateTime NotifyTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// 是否处于进出港报警阶段
        /// </summary>
        public bool IsOutstanding
        {
            get { return OutstandTime.AddSeconds(ConfigDataMgr.TrackTimeout) > DateTime.Now; }
        }

        public bool IsTimeout(ShipSignalType signal)
        {
            if (IsMovedTimeout())
                return true;
            //如果船舶信号类别确认刷新是否超时，返回对应信号的超时状态。
            switch (signal)
            {
                case ShipSignalType.Signal:
                    return Ship.UpdateTime.AddSeconds(ConfigDataMgr.SignalTimeout) < DateTime.Now;
                case ShipSignalType.AISTrack:
                    return Ship.UpdateTime.AddSeconds(ConfigDataMgr.TrackTimeout) < DateTime.Now;
                case ShipSignalType.RadarTrack:
                    return Ship.UpdateTime.AddSeconds(ConfigDataMgr.RadarTimeout) < DateTime.Now;
                default:
                    return false;
            }
        }
        ///如果船舶离开区域且达到一定的时间，返回true
        public bool IsMovedTimeout()
        {
            return (IsMoved && MoveOutTime.AddSeconds(ConfigDataMgr.TrackTimeout) < DateTime.Now);
        }

        public bool IsMoved { get { return MoveOutTime != DateTime.MinValue; } }

        public ShipTrackConfig(ShipInfo ship, bool isAlarm)
        {
            Update(ship, isAlarm, false);
        }

        public void Update(ShipInfo ship, bool isAlarm)
        {
            Ship = ship;
            if (isAlarm)
            {
                OutstandTime = ship.UpdateTime;
                IsOutstanded = false;
            }
            else if (!IsOutstanding && OutstandTime != DateTime.MinValue)//报警阶段结束且存在报警记录，此时清空报警
            {
                OutstandTime = DateTime.MinValue;
                IsOutstanded = false;
            }
        }

        public void Update(ShipInfo ship, bool isAlarm, bool isOut)
        {
            Update(ship, isAlarm);
            if (isOut && MoveOutTime == DateTime.MinValue)
                MoveOutTime = ship.UpdateTime;
            else if (!isOut && MoveOutTime != DateTime.MinValue)
                MoveOutTime = DateTime.MinValue;
        }

        public enum ShipSignalType
        {
            Signal,
            AISTrack,
            RadarTrack,
        }

    }
}
