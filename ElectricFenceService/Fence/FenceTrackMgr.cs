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

        List<GateTrackInfo>  _tracks = new List<GateTrackInfo>();//记录当前所有跟踪信息
        SocketsListener _listener;
        FenceTrackMgr()
        {
        }

        public void Start(int port)
        {
            _listener = new SocketsListener();
            _listener.Start(port);
        }

        public void Update()
        {
            do
            {
                var trackId = Track.TrackEventMgr.Instance.Dequeue();
                if (string.IsNullOrEmpty(trackId))
                    break;
                //获取当前变更船舶对应的所有区域跟踪列表(内部区域排在前面)
                var regions = ShipFenceMgr.Instance.Regions.Where(_ => _.Tracks.ContainsKey(trackId)).OrderBy(_=>_.IsInner);
                removeInvalidTrack(regions,trackId);//删除并处理无效跟踪，当前只有超时认为无效（信号超时或离开区域达到一定时间）
                if (regions.Count() > 0)
                {//找到对应的区域列表
                    var reg = regions.FirstOrDefault();
                    string[] gates = reg.GateIds;//获取区域闸机列表
                    var gateTracks = _tracks.Where(_ => gates.Any(g => g == _.GateId)).ToArray();//获取当前区域所有闸机的跟踪状态
                    reg.UpdateTrack(gateTracks, trackId);// 此时，找到船舶对应最优先的区域，对该船进行下一步跟踪操作
                }
            }
            while (true);
        }
        /// <summary>处理所有无效跟踪</summary>
        /// <param name="regions">当前船舶所在的区域列表</param>
        /// <param name="trackId">当前船舶ID</param>
        private void removeInvalidTrack(IEnumerable<PolygonTrack> regions,string trackId)
        {
            //获取当前船舶所有的历史跟踪记录
            var historys = _tracks.Where(_ => _.ShipID == trackId).ToArray();
            for (int i = 0; i < historys.Length; i++)
            {//遍历历史跟踪闸机，检查每个闸机所在区域是否可能在跟踪该船，若全不在跟踪，则说明该跟踪记录将结束
                var histrack = historys[i];
                var gateId = histrack.GateId;//上一次跟踪闸机编号
                if (regions.All(_ => _.GateIds.All(g => g != gateId)))//该船当前所有所在区域中均不含之前跟踪的闸机，即没有任何区域在监视这条船
                {
                    Common.Log.Logger.Default.Trace($"结束闸机 {histrack.GateId} 对船舶 {histrack.ShipID} {histrack.Ship.Ship.Name} 的跟踪");
                    _tracks.Remove(histrack);
                    if (histrack.TrackStatus != GateTrackStatus.OuterTrack)//仅考虑被删除的为外围跟踪的情况
                        continue;
                    onTrack(histrack, true);
                }
            }
        }

        public void UpdateTrack(GateTrackInfo track)
        {
            var last = _tracks.FirstOrDefault(t => t.GateId == track.GateId);
            if (last != null)
            {
                last.Update(track.Ship);
                last.TrackStatus = track.TrackStatus;
                last = track;
            }
            else
                _tracks.Add(track);
            onTrack(track);
        }

        void onTrack(GateTrackInfo track, bool isRemoved = false)
        {
            //Common.Log.Logger.Default.Trace($"track:{track.GateId} {track.Gate.Name} {track.ShipID} {track.Ship.Ship.Name} - 状态： {track.TrackStatus} 报警时间: {track.Ship.OutstandTime} 已报警？ {track.Ship.IsOutstanded}");
            var gatetrack = track.GetGateTrack();
            if (gatetrack != null)
            {
                if ((isRemoved && gatetrack.msgType == "4") || (!isRemoved && gatetrack.msgType != "4"))
                {
                    string strJson = gatetrack.ToJson();
                    _listener?.Send(strJson);

                    Common.Log.Logger.Default.Trace("send: track " + strJson);
                    Console.WriteLine($"正在跟踪 - {strJson}");
                    if(gatetrack.msgType == "1" || gatetrack.msgType == "2")//完成进出港报警
                        track.Ship.IsOutstanded = true;
                    track.Ship.NotifyTime = track.Ship.Ship.UpdateTime;
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
    }
}
