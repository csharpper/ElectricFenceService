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

        public void UpdateTrack(PolygonInfo poly, ShipInfo ship, bool isInRegion)
        {
            lock (_track)
            {
                var history = _track.FirstOrDefault(_ => poly.RegionId == _.RegionId && _.Ship.ID == ship.ID);//查找该区域内是否有闸机在跟踪
                if (isInRegion)//船舶位置在区域内
                {
                    if (history != null)
                    {
                        GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(history.GateId) as GateInfo;
                        if (poly.IsInner)//该区域为内部区域
                        {//不刷新，或修改为15分钟刷新一次
                            //if (history.LastestTime > history.TrackTime.AddSeconds(Heartbeat))
                                updateTrack(history, ship, poly.IsInner);
                            //else
                            //{//内部区域的目标，且该目标之前已刷新，此时不刷新。
                            //    //Common.Log.Logger.Default.Trace(GateTrack.Add(ship, gate).ToJson());
                            //    Common.Log.Logger.Default.Trace($"{ship.MMSI} - {ship.Name} 内部区域的目标，且该目标之前已刷新，此时不刷新 {poly.RegionId} - {poly.Name}");
                            //    Console.WriteLine($"{DateTime.Now.TimeOfDay} {ship.MMSI} - {ship.Name} 内部区域的目标，且该目标之前已刷新，此时不刷新 {poly.RegionId} - {poly.Name}");
                            //}

                        }
                        else
                            updateTrack(history, ship, poly.IsInner);
                    }
                    else
                    {
                        tryTrack(poly,ship);//尝试跟踪船舶
                    }
                }
                else//船舶位置不在区域内
                {
                    var oldtrack = _track.FirstOrDefault(_ => _.Ship.ID == ship.ID && _.RegionId == poly.RegionId);
                    if (oldtrack != null)
                    {
                        onLeaved(oldtrack, ship, poly.IsInner);
                    }
                }
            }
        }

        /// <summary>
        /// 此处为船舶进入区域新记录，尝试添加球机进行跟踪
        /// </summary>
        /// <param name="ship"></param>
        private void tryTrack(PolygonInfo poly, ShipInfo ship)
        {
            string[] gates = poly.GateIds;//获取当前区域所有闸机列表
            if (gates != null && gates.Length > 0)
            {//遍历闸机列表，找到第一个空闲的闸机
                int index = 0;
                while (index < gates.Length)
                {
                    var gate = gates[index];
                    if (_track.All(_ => _.GateId != gate))//当前的闸机不在正在跟踪的闸机列表中
                    {
                        var track = new GateTrackInfo(poly.RegionId, poly.IsInner, gate, ship);
                        _track.Add(track);
                        onTrack(track);
                        break;
                    }
                    index++;
                }
                if (index >= gates.Length)//无空闲闸机可用时，找到正在跟踪列表中优先级低于当前优先级的闸机进行跟踪
                {
                    var trackings = _track.Where(track => gates.Any(_ => _ == track.GateId));//获取当前区域对应闸机的使用情况
                    if (poly.IsInner)//如果新数据为内部区域，则只需要考虑替换内部区域的闸机
                        trackings = trackings.Where(track => track.IsInner);
                    ///获取优先级最低的一个跟踪记录，与当前船舶进行比较
                    if (!tryReplace(trackings, ship, poly.IsInner))
                    {//未找到可使用的闸机跟踪船舶
                        Common.Log.Logger.Default.Trace($"{poly.RegionId} - {poly.Name} 未找到可用的闸机跟踪船舶 {ship.MMSI} - {ship.Name}");
                        Console.WriteLine($"{DateTime.Now.TimeOfDay} {poly.RegionId} - {poly.Name} 未找到可用的闸机跟踪船舶 {ship.MMSI} - {ship.Name}");
                    }
                }
            }
        }
        /// <summary>
        /// 查找并替换低于该优先级的跟踪闸机
        /// </summary>
        bool tryReplace(IEnumerable<GateTrackInfo> tracks, ShipInfo ship, bool isInner)
        {
            if (tracks.Count() == 0)
                return false;
            var min = tracks.Min(_=>_.Priority);//获取优先级低于
            int shipPrior = GateTrackInfo.GetPriority(ship, isInner);
            if (min < shipPrior)
            {
                var track = tracks.FirstOrDefault(_ => _.Priority == min);
                updateTrack(track, ship, isInner);
                return true;
            }
            else
                Common.Log.Logger.Default.Trace($"未找到优先级较低的跟踪闸机。{min} >= {shipPrior}");
            return false;
        }

        /// <summary>
        /// 发送指定闸机跟踪指定船舶
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ship"></param>
        private void updateTrack(GateTrackInfo track, ShipInfo ship, bool isInner)
        {
            track.Update(ship, isInner);
            onTrack(track);
        }

        private void onTrack(GateTrackInfo track)
        {
            track.TrackTime = DateTime.Now;
            ///此处添加对跟踪目标跟踪相关的内容
            GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(track.GateId) as GateInfo;
            string strJson = GateTrack.Add(track.Ship, gate, track.IsInner).ToJson();
            _listener?.Send(strJson);
            Common.Log.Logger.Default.Trace("send: track " + strJson);
            Console.WriteLine($"正在跟踪 - {strJson}");
        }

        private void onLeaved(GateTrackInfo track, ShipInfo ship, bool isInner)
        {
            _track.Remove(track);
            GateInfo gate = FenceMgr.Instance.Fence.Get<GateInfo>(track.GateId) as GateInfo;
            var info = GateTrack.Add(ship, gate, isInner, "2").ToJson();
            Common.Log.Logger.Default.Trace("send: leave " + info);
            _listener?.Send(info);
            Common.Log.Logger.Default.Trace($"{ship.MMSI} - {ship.Name} 船舶离开区域 {track.RegionId},结束闸机 {track.GateId}");
            Console.WriteLine($"{DateTime.Now.TimeOfDay} {ship.MMSI} - {ship.Name} 船舶离开区域 {track.RegionId},结束闸机 {track.GateId}");
        }
    }

}
