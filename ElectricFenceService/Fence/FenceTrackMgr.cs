using ElectricFenceService.Listen;
using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class FenceTrackMgr
    {
        public readonly static FenceTrackMgr Instance = new FenceTrackMgr();
        const int Heartbeat = 15 * 60;//心跳，内部区域船舶至少间隔15分钟一个心跳包
        
        List<GateTrackInfo> _track = new List<GateTrackInfo>();//记录当前所有跟踪信息
        SocketsListener _listener;
        FenceTrackMgr()
        {
        }

        public void Start(int port)
        {
            _listener = new SocketsListener();
            _listener.Start(port);
        }

        public void UpdateTrack(PolygonInfo poly, ShipInfo ship, bool isInRegion,bool isChanged)
        {
            lock (_track)
            {
                if (poly.IsInner && isInRegion)//船舶位置在内部区域内
                    updateInInner(poly, ship, isChanged);
                else if (!poly.IsInner && isInRegion)//船舶位置在外围区域内
                    updateInOuter(poly, ship);
                else if (poly.IsInner && !isInRegion)//船舶离开内部区域
                    updateOutInner(poly, ship);
                else//离开外部区域
                    updateOutOuter(poly, ship);
            }
        }

        private void updateOutOuter(PolygonInfo poly, ShipInfo ship)
        {
            var historys = _track.Where(_ => poly.RegionId == _.RegionId && isSameShip(_.Ship, ship)).ToArray();//查找该区域内所有在跟踪该船的闸机列表
            if (historys.Count()> 0)
            {
                foreach (var history in historys)
                    onLeaved(history, ship, false, false);
            }
        }

        bool isSameShip(ShipInfo ship1, ShipInfo ship2)
        {
            return ship1.ID == ship2.ID || (ship1.MMSI != 0 && ship1.MMSI == ship2.MMSI);
        }

        /// <summary>
        /// 当船舶离开内部区域时
        /// </summary>
        private void updateOutInner(PolygonInfo poly, ShipInfo ship)
        {
            var historys = _track.Where(_ => poly.RegionId == _.RegionId).ToArray();//查找该区域内所有在跟踪的闸机
            int count = historys.Count();//记录内部区域中跟踪的闸机总数
            if (count > 0)
            {//更新所有在跟踪内部区域船舶的闸机,推送其状态变化
                foreach (var history in historys)
                {
                    GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(history.GateId) as GateInfo;
                    onLeaved(history, ship, true,poly.IsTracking);
                }
            }
        }

        /// <summary>
        /// 当船舶在内部区域内时
        /// </summary>
        void updateInInner(PolygonInfo poly, ShipInfo ship, bool isChanged)
        {
            var historys = _track.Where(_ => poly.RegionId == _.RegionId).ToArray();//查找该区域内所有在跟踪的闸机
            int count = historys.Count();//记录内部区域中跟踪的闸机总数
            if (count > 0)
            {//更新所有在跟踪内部区域船舶的闸机,推送其状态变化
                foreach (var history in historys)
                {
                    GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(history.GateId) as GateInfo;
                    updateTrack(history, ship, poly, isChanged);
                }
            }

            //调用所有空闲的闸机
            string[] frees = getFreeGates(poly);
            count += frees.Length;
            foreach (var gateId in frees)
            {
                //GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(gateId) as GateInfo;
                var track = new GateTrackInfo(poly.RegionId, poly.IsInner, gateId, ship);
                _track.Add(track);
                onTrack(track, isChanged);
            }
            if (count == 0)//说明没有闸机在跟踪内部区域，此时，抢占外围区域的闸机
            {
                string[] gates = poly.GateIds;//获取当前区域所有闸机列表
                var trackings = _track.Where(track => gates.Any(_ => _ == track.GateId));//获取当前区域对应闸机的使用情况
                tryReplace(trackings, ship, poly, isChanged);
            }
        }

        private void updateInOuter(PolygonInfo poly, ShipInfo ship)
        {
            ///优先调用前一次跟踪该船的闸机
            var history = _track.FirstOrDefault(_ => poly.RegionId == _.RegionId && isSameShip(_.Ship, ship));//查找该区域内是否有闸机在跟踪该船
            if (history != null)
            {
                updateTrack(history, ship, poly, false);//保持跟踪
                return;
            }
            //其次调用空闲的闸机（优先级最高的一个）
            string[] frees = getFreeGates(poly);
            if (frees.Length > 0)//选取第一个空闲的闸机进行跟踪
            {
                var track = new GateTrackInfo(poly.RegionId, poly.IsInner, frees.First(), ship);
                _track.Add(track);
                updateTrack(track, ship, poly, false);
                return;
            }
            //再次，选取可用的闸机
            string[] gates = poly.GateIds;//获取当前区域所有闸机列表
            var trackings = _track.Where(track => gates.Any(_ => _ == track.GateId));//获取当前区域对应闸机的使用情况
            ///当没有空闲闸机时，优先选取多余的内部区域跟踪闸机
            var inners = trackings.Where(_ => _.IsInner);//正在跟踪内部区域的闸机列表
            if (inners.Count() > 1)//超过一个在跟踪内部区域的闸机
            {
                var inner = inners.First();
                if (inner.Ship.MMSI == ship.MMSI)
                {//同一个闸机继续跟踪（由内部区域改为外围区域）
                    onLeaved(inner, ship, true, true);//通知离开内部区域，但不删除跟踪，一遍外部区域继续跟踪
                }
                inner.RegionId = poly.RegionId;
                updateTrack(inner, ship, poly, false);
                return;
            }
            //最后，找到所有外围在跟踪的列表，抢占优先级低的跟踪
            var outers = trackings.Where(_ => !_.IsInner);
            tryReplace(outers, ship, poly, false);
        }

        string[] getFreeGates(PolygonInfo poly)
        {
            List<string> frees = new List<string>();
            string[] gates = poly.GateIds;
            if (gates != null && gates.Length > 0)
            {
                for (int i = 0; i < gates.Length; i++)
                {
                    var gate = gates[i];
                    if (_track.All(_ => _.GateId != gate))//找到空闲闸机
                        frees.Add(gate);
                }
            }
            return frees.ToArray();
        }
        
        /// <summary>
        /// 查找并替换低于该优先级的跟踪闸机
        /// </summary>
        bool tryReplace(IEnumerable<GateTrackInfo> tracks, ShipInfo ship, PolygonInfo poly, bool isChanged)
        {
            if (tracks.Count() == 0)
                return false;
            ///查找外围跟踪超时的闸机进行跟踪
            var timeouttrack = tracks.FirstOrDefault(_ => _.IsTimeout && !_.IsInner);
            if (timeouttrack != null)
            {
                updateTrack(timeouttrack, ship, poly, isChanged);
                return true;
            }
            ///尝试调用低优先级的船舶跟踪闸机进行跟踪
            var min = tracks.Min(_=>_.Priority);//获取优先级最低值
            int shipPrior = GateTrackInfo.GetPriority(ship, poly.IsInner);
            if (min < shipPrior)
            {
                var track = tracks.FirstOrDefault(_ => _.Priority == min);
                updateTrack(track, ship, poly, isChanged);
                return true;
            }
            else if (min == shipPrior)
            {
                var track = tracks.FirstOrDefault(_ => _.Priority == min);
                if (track.Ship.MMSI == ship.MMSI && ship.MMSI != 0)//是否为同一条船
                    updateTrack(track, ship, poly, isChanged);
                else
                    Common.Log.Logger.Default.Error($"未找到优先级较低的跟踪闸机。{track.RegionId} {ship.MMSI} - {ship.Name} {min} == {shipPrior} {track.Ship.MMSI},{track.Ship.Name}");
            }
            else
                Common.Log.Logger.Default.Trace($"未找到优先级较低的跟踪闸机。{tracks?.FirstOrDefault()?.RegionId} {ship.MMSI} - {ship.Name} {min} > {shipPrior}");
            return false;
        }

        /// <summary>
        /// 发送指定闸机跟踪指定船舶
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ship"></param>
        private void updateTrack(GateTrackInfo track, ShipInfo ship, PolygonInfo poly, bool isChanged)
        {
            track.Update(ship, poly.RegionId, poly.IsInner);
            onTrack(track, isChanged);
        }

        private void onTrack(GateTrackInfo track, bool isChanged = false)
        {
            track.TrackTime = DateTime.Now;
            ///此处添加对跟踪目标跟踪相关的内容
            GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(track.GateId) as GateInfo;
            string strJson = null;
            if(track.IsInner)
                strJson= GateTrack.Add(track.Ship, gate, true, isChanged ? "1" : "3").ToJson();//首次进入内部区域的标记状态为1,心跳标记为3
            else
                strJson = GateTrack.Add(track.Ship, gate, false, "").ToJson();//外部区域，正常发送
            _listener?.Send(strJson);
            Common.Log.Logger.Default.Trace("send: track " + strJson);
            Console.WriteLine($"正在跟踪 - {strJson}");
        }

        private void onLeaved(GateTrackInfo track, ShipInfo ship, bool isInner,bool isTracking = false)
        {
            if(!isInner || !isTracking)//加入判断是考虑内部区域有其他船舶在跟踪时
                _track.Remove(track);
            GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(track.GateId) as GateInfo;
            //if (!isInner)//非内部区域，不返回状态
            //{
            //    Console.WriteLine($"{DateTime.Now.TimeOfDay} {ship.MMSI} - {ship.Name} 船舶离开外围区域 {track.RegionId},结束闸机 {track.GateId}");
            //    return;
            //}
            var info = GateTrack.Add(ship, gate, isInner, isInner?"2":"4").ToJson();//离开内部区域，返回2，离开外部区域，返回4
            _listener?.Send(info);
            Common.Log.Logger.Default.Trace($"{ship.MMSI} - {ship.Name} 船舶离开区域 {track.RegionId},结束闸机 {track.GateId} - {isInner}");
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {ship.MMSI} - {ship.Name} 船舶离开区域 {track.RegionId},结束闸机 {track.GateId}");
        }
    }
}
