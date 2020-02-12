using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class ShipFenceMgr
    {
        public readonly static ShipFenceMgr Instance = new ShipFenceMgr();
        List<PolygonInfo> _regions = new List<PolygonInfo>();//记录区域的列表,区域关联的闸机，并将区域重新封装，用于点是否在区域中的判断。
        Dictionary<string, ShipInfo> _shipDicts = new Dictionary<string, ShipInfo>();//记录所有船舶最后一条数据
        Dictionary<string, List<string>> _regionShips = new Dictionary<string, List<string>>();//设置关联船舶，记录每个区域内关联的所有船舶
        ShieldData _shield = null;
        object _obj = new object();
        ShipFenceMgr()
        {
            load();
            FenceMgr.Instance.FenceChanged += load;
            new Thread(run) { IsBackground = true }.Start();
        }

        Queue<ShipInfo> _shipQueue = new Queue<ShipInfo>();
        private void run()
        {
            while (true)
            {
                ShipInfo ship = null;
                lock (_shipQueue)
                {
                    if (_shipQueue.Count > 0)
                    {
                        if (_shipQueue.Count > 100)
                            Common.Log.Logger.Default.Trace("数据拥堵: " + _shipQueue.Count);
                        ship = _shipQueue.Dequeue();
                    }
                }
                if(ship != null)
                    update(ship);
                else
                    Thread.Sleep(1);
                if(_updateTime.AddSeconds(10) < DateTime.Now)//每10秒清理一次超时目标
                    updateTracking();
            }
        }

        public void Init()
        {
        }

        private void load()
        {//更新区域
            lock (_regions)
            {
                Shield.ShieldMgr.Instance.ShieldChanged += onShield;
                onShield();
                var bridges = FenceMgr.Instance.Fence.Bridges;
                var regions = FenceMgr.Instance.Fence.Regions;
                ///删除过期的区域
                var deleted = _regions.Where(_ => regions.FirstOrDefault(d => d.ID == _.RegionId) == null).ToArray();
                if(deleted != null && deleted.Count() > 0)
                {
                    foreach (var d in deleted)
                    {
                        _regions.Remove(d);
                    }
                }

                foreach (var reg in _regions)
                {//更新已有的区域
                    var newDt = regions.FirstOrDefault(_ => _.ID == reg.RegionId);
                    if (newDt != null)
                    {
                        var gates = FenceMgr.Instance.Fence.GetGateIdsFromRegion(reg.RegionId);
                        reg.UpdateRegion(newDt, gates);
                    }
                    else
                    {
                        Common.Log.Logger.Default.Error($"区域更新失败，{reg.Name}未找到更新项，该日志说明软件该模块代码错误。");
                    }
                }

                ///更新已有的区域，创建新增的区域
                foreach (var newReg in regions)
                {//未完成，此处未考虑闸机的优先级
                    var last = _regions.FirstOrDefault(_ => _.RegionId == newReg.ID);
                    var gateIds = FenceMgr.Instance.Fence.GetGateIdsFromRegion(newReg.ID); 
                    if (last != null)
                        last.UpdateRegion(newReg, gateIds);
                    else
                    {
                        var reg = new PolygonInfo(newReg, gateIds);
                        _regions.Add(reg);
                    }
                }
                _regions = _regions.OrderBy(_ => _.IsInner).ToList();
            }
        }

        private void onShield()
        {
            lock(_obj)
                _shield = Shield.ShieldMgr.Instance.GetShield();
            Common.Log.Logger.Default.Trace($"------------------------");
            string str = $"更新当前围栏屏蔽数据。其中船舶数量：{_shield?.Ships?.Count()},AIS类别数量：{_shield?.Types?.Count()}";
            if(_shield?.Ships!= null && _shield.Ships.Count > 0)
            {
                str += "。其中船舶MMSI：";
                foreach (var sh in _shield.Ships)
                    str += " " + sh.ID;
            }
            if (_shield?.Types != null && _shield.Types.Count > 0)
            {
                str += "。其中类别：";
                foreach (var sh in _shield.Types)
                    str += " " + sh.ID;
            }
            Common.Log.Logger.Default.Trace(str);
            Common.Log.Logger.Default.Trace($"------------------------");
        }

        public void Update(ShipInfo ship)
        {
            if (isShieldShip(ship))
            {
                Console.WriteLine($"{ship.MMSI}-{ship.Name}-{ship.ShipCargoType} - 屏蔽船舶");
                return;
            }
            lock(_shipQueue)
                _shipQueue.Enqueue(ship);
        }

        private bool isShieldShip(ShipInfo ship)
        {
            lock (_obj)
            {
                if (_shield == null)
                    return false;
                return _shield.IsShield(ship);
            }
        }

        public /*async*/ void update(ShipInfo info)
        {
            bool isFirstData = true;
            string shipId = info.MMSI != 0 ? info.MMSI.ToString() : info.ID;
            lock (_shipDicts)
            {
                if ( _shipDicts.ContainsKey(shipId) && PolygonInfo.IsTimeout(_shipDicts[shipId]))//该船舶刷新超时
                    isFirstData = false;
                _shipDicts[shipId] = info;
            }
            //await Task.Yield();
            //if(info.MMSI ==0)
            //    Common.Log.Logger.Default.Trace($"##################{info.ID}，{info.MMSI}:{info.Name} 雷达目标，不考虑雷达进入内部区域。");
            _regions.ForEach(_=>_.Update(info, isFirstData));
            //Parallel.For(0, _regions.Count, i => _regions[i].Update(info));
        }

        DateTime _updateTime = DateTime.Now;
        void updateTracking()
        {
            _regions.ForEach(_=>_.RemoveTimeoutTrack());
            _updateTime = DateTime.Now;
        }
    }
}
