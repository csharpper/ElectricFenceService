using Fence.Util;
using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class PolygonInfo
    {
        public string RegionId { get; private set; }//区域的ID
        public string Name { get; private set; }
        public bool IsInner { get; private set; }
        public PolygonD Regions { get; private set; }
        Dictionary<string, ShipInfo> Tracks = new Dictionary<string, ShipInfo>();//记录当前区域所有船舶列表
        public bool IsTracking { get { return Tracks.Count > 0; } }
        public Action<PolygonInfo, ShipInfo, bool,bool> TrackChanged { get; set; }
        public string[] GateIds { get; private set; }
        object _obj = new object();
        DateTime _startTime = DateTime.Now;
        static int _timeoutMinutes = 15;
        static PolygonInfo()
        {
            int.TryParse(ConfigData.TimeoutMinutes, out _timeoutMinutes);
            Common.Log.Logger.Default.Trace($"超时时间：{_timeoutMinutes}分钟。");
            if (_timeoutMinutes <= 0)
                _timeoutMinutes = 15;
        }
        public PolygonInfo(FenceRegionsInfo info,string[] gateIds)
        {
            Common.Log.Logger.Default.Trace($"region:{info.Name} 闸机总数：" + gateIds?.Length);
            RegionId = info.ID;
            UpdateRegion(info,gateIds);
        }

        public static bool IsTimeout(ShipInfo ship)
        {
            if(ship.MMSI > 0)
                return ship.UpdateTime.AddMinutes(_timeoutMinutes) < DateTime.Now;
            else
                return ship.UpdateTime.AddSeconds(15) < DateTime.Now;//10秒清除雷达跟踪目标
        }

        public void RemoveTimeoutTrack()
        {
            lock (Tracks)
            {
                var timeouts = Tracks.Where(_ => IsTimeout(_.Value)).ToArray();//所有超时的区域目标；
                if (timeouts.Length > 0)
                {
                    Common.Log.Logger.Default.Trace($"Region {RegionId} {Name} 存在目标刷新超时，准备清除超时目标，清除总数:{timeouts.Length}");
                    foreach (var del in timeouts)
                    {
                        if (!IsInner)
                        {
                            var ship = del.Value;
                            Common.Log.Logger.Default.Trace($"@@@@@@@@@@@@@@@{ship.ID}，{ship.MMSI}:{ship.Name} 目标超时 {RegionId} {Name} - {IsInner}- 当前船舶总数 {Tracks.Count}");
                            onTrack(ship, false, false);//通知外部该船已不在该区域内
                        }
                        Tracks.Remove(del.Key);
                    }
                    Common.Log.Logger.Default.Trace($"清除完成。");
                }
            }
        }

        public void Update(ShipInfo ship, bool isFirstData)
        {
            string shipID = ship.MMSI == 0 ? ship.ID : ship.MMSI.ToString();
            if(IsInner && ship.MMSI == 0)//不考虑雷达目标在内部区域时
            {
                //Common.Log.Logger.Default.Trace($"##################{ship.ID}，{ship.MMSI}:{ship.Name} InRegion {RegionId} {Name} - {IsInner},雷达目标，不考虑雷达进入内部区域。");
                return;
            }
            lock (_obj)
            {
                bool inRegion = Calculator.PtInPolygon(ship.Longitude, ship.Latitude, Regions);
                if (inRegion)
                {
                    bool isFirst = false;
                    if (!isFirstData)
                    {//不考虑首次刷新的内部区域的船舶（此时认为该船不是刚进入区域）
                        lock (Tracks)
                            isFirst = (IsInner && !Tracks.ContainsKey(shipID));//内部区域中首次出现
                    }
                    else
                        Common.Log.Logger.Default.Trace($"首次进入区域，不判断该船舶进港状态。");
                    lock (Tracks)
                        Tracks[shipID] = ship;
                    Common.Log.Logger.Default.Trace($"---------------{ship.ID}，{ship.MMSI}:{ship.Name} InRegion {RegionId} {Name} - {IsInner}. 第一次： {isFirst} - 船舶总数 {Tracks.Count}");
                    onTrack(ship, true, isFirst);
                }
                else
                {
                    lock (Tracks)
                    {
                        if (Tracks.ContainsKey(shipID))//获取该船之前是否在区域内
                        {
                            Tracks.Remove(shipID);
                            Common.Log.Logger.Default.Trace($"+++++++++++++++++{ship.ID},{ship.MMSI}:{ship.Name} OutRegion {RegionId} {Name}-{IsInner}. - 船舶总数 {Tracks.Count}");
                            onTrack(ship, false, IsInner);
                        }
                    }
                }
            }
        }

        public void UpdateRegion(FenceRegionsInfo info,string[] gateIds)
        {
            lock (_obj)
            {
                Name = info.Name;
                IsInner = info.IsInner;
                GateIds = gateIds;
                Regions = new PolygonD();
                Regions.AddPoints(new PointDArray(info.Region));
            }
        }

        void onTrack(ShipInfo ship, bool isInRegion,bool isFirstChanged)
        {
            FenceTrackMgr.Instance.UpdateTrack(this, ship, isInRegion, isFirstChanged);
            ///TrackChanged?.Invoke(this,ship, isInRegion);
        }
    }
}
