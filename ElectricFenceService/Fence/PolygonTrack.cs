using ElectricFenceService.Track;
using Fence.Util;
using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class PolygonTrack
    {
        public string RegionId { get; private set; }//区域的ID
        public string Name { get; private set; }
        public bool IsInner { get; private set; }
        public PolygonD Regions { get; private set; }
        public Dictionary<string, ShipTrackConfig> Tracks { get; } = new Dictionary<string, ShipTrackConfig>();//记录当前区域所有船舶列表及船舶状态
        public bool IsTracking { get { return Tracks.Count > 0; } }
        public string[] GateIds { get; private set; }
        object _obj = new object();

        public PolygonTrack(FenceRegionsInfo info,string[] gateIds)
        {
            UpdateRegion(info,gateIds);
        }

        public void RemoveTimeoutTrack()
        {
            lock (_obj)
            {
                var timeouts = Tracks.Where(_ =>isTimeout(_.Value)).ToArray();//所有超时的区域目标；
                if (timeouts.Length > 0)
                {
                    trace($"--------清除区域 {RegionId} {Name} 超时目标，总数:{Tracks.Count}-{timeouts.Length}--------");
                    foreach (var del in timeouts)
                    {
                        var ship = del.Value.Ship;
                        string shipID = ship.MMSI == 0 ? ship.ID : ship.MMSI.ToString();

                        Tracks.Remove(shipID);
                        TrackEventMgr.Instance.Enqueue(shipID);
                        trace($"船舶{shipID} {ship.Name} 跟踪超时，所在区域 {RegionId} {Name}-内部区域 {IsInner}.");
                    }
                }
            }
        }

        public void Update(ShipInfo ship, bool isFirstData)
        {
            string shipID = ship.MMSI == 0 ? ship.ID : ship.MMSI.ToString();
            if (IsInner && ship.MMSI == 0)//不考虑雷达目标在内部区域
                return;
            lock (_obj)
            {
                ShipTrackConfig lastTrack = null;
                if (Tracks.ContainsKey(shipID))//找到该船之前的跟踪记录
                    lastTrack = Tracks[shipID];

                bool inRegion = Calculator.PtInPolygon(ship.Longitude, ship.Latitude, Regions);
                if (inRegion)
                {
                    bool isAlarm = !isFirstData && IsInner && lastTrack == null;//不考虑首次刷新的内部区域的船舶（此时认为该船不是刚进入区域
                    if (lastTrack == null)//无历史跟踪数据
                        Tracks[shipID] = new ShipTrackConfig(ship, isAlarm);
                    else
                        lastTrack.Update(ship, isAlarm, false);
                    trace($"-----------船舶 {ship.ID}:{ship.Name} 进入区域 {RegionId} {Name} - {IsInner}.报警：{isAlarm} - 跟踪总数 {Tracks.Count}");
                    TrackEventMgr.Instance.Enqueue(shipID);
                }
                else if (lastTrack != null)//当前船舶不在区域内,且该船历史在区域内跟踪
                {
                    if (!lastTrack.IsMoved || IsInner)//不考虑已经离开之后外围船舶的刷新
                    {
                        lastTrack.Update(ship, IsInner && !lastTrack.IsMoved, true);
                        TrackEventMgr.Instance.Enqueue(shipID);
                        trace($"+++++++++++++船舶 {shipID}：{ship.Name} 离开区域 {RegionId} {Name}-{IsInner}");
                    }
                }
            }
        }

        public void UpdateRegion(FenceRegionsInfo info, string[] gateIds)
        {
            lock (_obj)
            {
                Name = info.Name;
                IsInner = info.IsInner;
                GateIds = gateIds;
                Regions = new PolygonD();
                Regions.AddPoints(new PointDArray(info.Region));
                RegionId = info.ID;
                string gateStrs = "";
                if(GateIds!= null)
                    foreach (var gate in GateIds)
                        gateStrs += " - " + gate;
                trace($"更新区域，{Name} - 区域ID {RegionId} - 闸机数量 {GateIds?.Length}\r\n{gateStrs}。");
            }
        }

        bool isTimeout(ShipTrackConfig ship)
        {
            ShipTrackConfig.ShipSignalType type = ShipTrackConfig.ShipSignalType.AISTrack;
            if (ship.Ship.MMSI == 0)
                type = ShipTrackConfig.ShipSignalType.RadarTrack;
            else if (IsInner)
                type = ShipTrackConfig.ShipSignalType.Signal;
            return ship.IsTimeout(type);
        }

        void trace(string message)
        {
            Common.Log.Logger.Default.Trace(message);
        }

        #region 尝试推送目标的状态
        /// <summary>
        /// 通过该区域尝试推送跟踪状态
        /// </summary>
        /// <param name="tracks">当前闸机列表中正在跟踪的闸机状况</param>
        /// <param name="shipId"></param>
        public void UpdateTrack(GateTrackInfo[] tracks , string shipId)
        {
            lock (_obj)
            {
                if (IsInner)
                    updateInner(tracks, shipId);
                else
                    updateOuter(tracks, shipId);
            }
        }

        private void updateInner(GateTrackInfo[] tracks, string shipId)
        {
            var shipInfo = Tracks[shipId];
            if (shipInfo.IsOutstanding)//进出港报警时间段内
            {
                if (!shipInfo.IsOutstanded)//此前未向外提供报警信息，发送报警信息
                    updateAlarm(tracks, shipInfo, shipId);
                else//已经向外提供过进出港报警，此处刷新跟踪信息
                {
                    tryKeepTrack(tracks, shipInfo, shipId);//调用前一次跟踪该船的闸机继续跟踪
                }
            }
            else//非进出港报警阶段船舶内部数据刷新
                updateInInner(tracks, shipInfo);
        }

        /// <summary>在港船舶心跳</summary>
        private void updateInInner(GateTrackInfo[] tracks, ShipTrackConfig shipInfo)
        {
            if (shipInfo.MoveOutTime == DateTime.MinValue)//确保船舶数据在港内
            {
                string[] freeGates = getFreeGates(tracks);
                if (freeGates.Length > 0)
                {
                    trace($"_+_+{RegionId} {Name} 尝试刷新{freeGates.Length}个闸机。+_+_");
                    foreach (var free in freeGates)//刷新所有空闲的闸机
                        tryReplaceGate(tracks, shipInfo, free);
                }
                else
                    tryReplaceLowPrior(tracks, shipInfo, true);//此时内部区域无闸机可用，选择外围区域跟踪中优先级最低的闸机进行跟踪
            }
        }

        /// <summary>进出港报警推送</summary>
        private void updateAlarm(GateTrackInfo[] tracks, ShipTrackConfig shipInfo, string shipId)
        {
            if (shipInfo.OutstandTime != DateTime.MinValue)//进入区域报警，优先调用前一次跟踪该船的闸机
            {
                if (tryKeepTrack(tracks, shipInfo, shipId))//优先调用前一次跟踪该船的闸机
                    return;
            }
            //此时进出港报警且未找到正在跟踪的闸机，查找可用闸机并完成跟踪报警
            //调用空闲或可用的闸机（优先级最高的一个）
            string[] freeGates = getFreeGates(tracks);
            if (freeGates.Length > 0)//选取第一个空闲的闸机进行跟踪
                tryReplaceGate(tracks, shipInfo, freeGates.First());
            else //找到所有外围在跟踪的列表，抢占优先级低的跟踪
                tryReplaceLowPrior(tracks, shipInfo,true);
        }

        void updateOuter(GateTrackInfo[] tracks, string shipId)
        {
            var shipInfo = Tracks[shipId];
            if (tryKeepTrack(tracks, shipInfo, shipId))//优先调用前一次跟踪该船的闸机
                return;
            //调用空闲或可用的闸机（优先级最高的一个）
            string[] freeGates = getFreeGates(tracks);
            if (freeGates.Length > 0)//选取第一个空闲的闸机进行跟踪
            {
                string gate = freeGates.First();
                var firstTrack = tracks.FirstOrDefault(_ => _.GateId == gate);
                if (firstTrack == null || freeGates.Length > 1)
                {
                    updateReplaceGateUtil(firstTrack, shipInfo, gate);
                    return;
                }
            }
            tryReplaceLowPrior(tracks, shipInfo, false);//最后，找到所有外围在跟踪的列表，抢占优先级低的跟踪
        }

        /// <summary>
        /// 查找外围优先级最低的跟踪，判断是否替换该跟踪
        /// </summary>
        bool tryReplaceLowPrior(GateTrackInfo[] tracks, ShipTrackConfig ship, bool isCompulsory)
        {
            var outtracks = tracks.Where(_ => _.TrackStatus == GateTrackStatus.OuterTrack && !_.Ship.IsOutstanding).ToArray();
            if (outtracks.Count() == 0)
                return false;
            int shipPrior = ship.Ship.Priority;
            ///尝试调用低优先级的船舶跟踪闸机进行跟踪
            var min = outtracks.Min(_ => _.Ship.Ship.Priority);//获取优先级最低值
            if (isCompulsory || min < shipPrior)
            {
                var track = outtracks.LastOrDefault(_ => _.Ship.Ship.Priority == min);
                track.Update(ship);
                onTrack(track);
                return true;
            }
            else
                trace($"未找到优先级较低的闸机跟踪。{RegionId} {Name}: {ship.Ship.MMSI} - {ship.Ship.Name} {min} > {shipPrior}");
            return false;
        }

        void tryReplaceGate(GateTrackInfo[] tracks, ShipTrackConfig shipInfo, string gateId)
        {
            var lastTrack = tracks.FirstOrDefault(_ => _.GateId == gateId);
            updateReplaceGateUtil(lastTrack, shipInfo,gateId);
        }

        void updateReplaceGateUtil(GateTrackInfo lastTrack, ShipTrackConfig shipInfo, string gateId)
        {
            if (lastTrack == null)
            {
                var gateInfo = FenceMgr.Instance.Fence.Gates.FirstOrDefault(_ => _.ID == gateId);
                lastTrack = new GateTrackInfo(gateInfo, shipInfo);
            }
            else
                lastTrack.Update(shipInfo);
            onTrack(lastTrack);
        }

        bool tryKeepTrack(GateTrackInfo[] tracks, ShipTrackConfig ship, string shipId)
        {
            ///优先调用前一次跟踪该船的闸机
            var oldtrack = tracks.FirstOrDefault(track => track.ShipID == shipId);
            if (oldtrack != null)//找到之前正在跟踪的闸机，保持继续跟踪
            {
                oldtrack.Update(ship);
                onTrack(oldtrack);
            }
            return (oldtrack != null);
        }

        ///获取所有空闲闸机
        string[] getFreeGates(GateTrackInfo[] tracks)
        {
            List<string> frees = new List<string>();
            string[] gates = GateIds;
            if (gates != null && gates.Length > 0)
            {
                for (int i = 0; i < gates.Length; i++)
                {
                    var gate = gates[i];
                    var track = tracks.FirstOrDefault(_ => _.GateId == gate);
                    if(track == null)//该闸机处于闲置状态
                        frees.Add(gate);
                    else if (!track.Ship.IsOutstanding)//不处于报警阶段
                    {
                        if (track.TrackStatus == GateTrackStatus.InnerTrack && !track.Ship.IsMoved)//正在监视内部区域
                            frees.Add(gate);
                        else if (isTimeout(track.Ship))//跟踪超时的闸机
                            frees.Add(gate);
                    } 
                }
            }
            return frees.ToArray();
        }

        void onTrack(GateTrackInfo track)
        {
            if (IsInner)
                track.TrackStatus = GateTrackStatus.InnerTrack;
            else
                track.TrackStatus = GateTrackStatus.OuterTrack;
            FenceTrackMgr.Instance.UpdateTrack(track);
        }

        #endregion 尝试推送目标的状态
    }
}
