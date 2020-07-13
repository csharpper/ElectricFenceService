using ElectricFenceService.Track;
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
        public string GateId { get { return Gate.ID; } }
        public GateInfo Gate { get; }
        public ShipTrackConfig Ship { get; private set; }
        public string ShipID
        {
            get
            {
                if (Ship?.Ship == null)
                    return null;
                var ship = Ship.Ship;
                if (ship.MMSI > 0)
                    return ship.MMSI.ToString();
                return ship.ID;
            }
        }
        public GateTrackStatus TrackStatus { get; set; } = GateTrackStatus.UnWorking;

        public GateTrackInfo(GateInfo gate, ShipTrackConfig ship)
        {
            Gate = gate;
            Update(ship);
        }

        public void Update(ShipTrackConfig ship)
        {
            if (ShipID != ship.Ship.ID && ShipID != ship.Ship.MMSI.ToString())
                Ship = ship;
            else
            {
                bool isAlarm = (ship.OutstandTime == ship.Ship.UpdateTime && !ship.IsOutstanded) && Ship.OutstandTime != ship.OutstandTime;
                bool isOut = ship.MoveOutTime != DateTime.MinValue;
                bool lastIsOut = Ship.MoveOutTime != DateTime.MinValue;
                if (isOut != lastIsOut)
                    Ship.Update(ship.Ship, isAlarm, isOut);
                else
                    Ship.Update(ship.Ship, isAlarm);
            }
        }
        
        /// <summary>是否为内部区域停止的闸机跟踪</summary>
        public bool IsInInnered
        {//内部区域信号，不处于报警阶段，信号没有离开区域
            get { return TrackStatus == GateTrackStatus.InnerTrack && !Ship.IsOutstanding && Ship.MoveOutTime != DateTime.MinValue; }
        }

        public GateTrack GetGateTrack()
        {
            bool isOut = Ship.IsMoved;
            if (TrackStatus == GateTrackStatus.InnerTrack)
            {
                if (Ship.IsOutstanding)//报警阶段
                {
                    if (!Ship.IsOutstanded)//未发送过报警事件
                    {
                        if(!isOut)//进港报警
                            return GateTrack.Add(Ship.Ship, Gate, true, "1");
                        else//进港报警
                            return GateTrack.Add(Ship.Ship, Gate, true, "2");
                    }
                    if(isOut)//已经离开内部区域，发布外围跟踪消息
                        return GateTrack.Add(Ship.Ship, Gate, false, "");
                }
                if (!isOut)//非报警内部信号刷新，发送内部心跳
                    return GateTrack.Add(Ship.Ship, Gate, true, "3");
            }
            else//外围区域
            {
                if (!isOut)//外围区域内部
                    return GateTrack.Add(Ship.Ship, Gate, false, "");
                else
                    return GateTrack.Add(Ship.Ship, Gate, false, "4");
            }
            return null;
        }
    }

    public enum GateTrackStatus
    {
        UnWorking,//闲置的，未跟踪任何船舶
        InnerTrack,//内部区域跟踪
        OuterTrack,//外围自动跟踪
        HandTrack,//手动跟踪
    }
}
