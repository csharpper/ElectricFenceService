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
        public bool IsInner { get; set; }
        public PolygonD Regions { get; private set; }
        Dictionary<string, ShipInfo> Tracks = new Dictionary<string, ShipInfo>();//记录当前区域所有船舶列表
        public bool IsTracking { get { return Tracks.Count > 0; } }
        public Action<PolygonInfo, ShipInfo, bool,bool> TrackChanged { get; set; }
        public string[] GateIds { get; private set; }
        object _obj = new object();
        DateTime _startTime = DateTime.Now;
        public PolygonInfo(FenceRegionsInfo info,string[] gateIds)
        {
            Common.Log.Logger.Default.Trace($"region:{info.Name} 闸机总数：" + gateIds?.Length);
            RegionId = info.ID;
            UpdateRegion(info,gateIds);
        }

        public void Update(ShipInfo ship)
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
                    if (_startTime.AddSeconds(180) < DateTime.Now)
                    {//软件启动前3分钟不考虑新进入内部区域的船舶
                        lock (Tracks)
                        {
                            isFirst = (IsInner && !Tracks.ContainsKey(shipID));//内部区域中
                            Tracks[shipID] = ship;
                        }
                    }
                    //if(isFirst)
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
                removeTimeoutShip();
            }
        }

        private void removeTimeoutShip()
        {
            if (IsInner)
                return;//暂不删除内部区域超时目标
            lock (Tracks)
            {
                for (int index = 0; index < Tracks.Count;)
                {
                    var track = Tracks.ElementAt(index);
                    var ship = track.Value;
                    if (ship.UpdateTime.AddMinutes(15) < DateTime.Now || (ship.MMSI == 0 && ship.UpdateTime.AddSeconds(15) < DateTime.Now))//未超过十五分钟未更新,或雷达目标超过15秒未更新
                    {
                        Tracks.Remove(track.Key);
                        Common.Log.Logger.Default.Trace($"@@@@@@@@@@@@@@@{ship.ID}，{ship.MMSI}:{ship.Name} 目标超时 {RegionId} {Name} - {IsInner}- 当前船舶总数 {Tracks.Count}");
                        onTrack(ship, false, false);//通知外部该船已不在该区域内
                    }
                    else
                        index++;
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
