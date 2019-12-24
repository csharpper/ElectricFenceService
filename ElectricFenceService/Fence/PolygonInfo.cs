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
        public Action<PolygonInfo, ShipInfo, bool> TrackChanged { get; set; }
        public string[] GateIds { get; private set; }
        object _obj = new object();
        public PolygonInfo(FenceRegionsInfo info,string[] gateIds)
        {
            RegionId = info.ID;
            UpdateRegion(info,gateIds);
        }

        public void Update(ShipInfo ship)
        {
            lock (_obj)
            {
                bool inRegion = Calculator.PtInPolygon(ship.Longitude, ship.Latitude, Regions);
                if (inRegion)
                {
                    Tracks[ship.ID] = ship;
                    onTrack(ship, true);
                }
                else
                {
                    if (Tracks.ContainsKey(ship.ID))//获取该船之前是否在区域内
                    {
                        Tracks.Remove(ship.ID);
                        onTrack(ship, false);
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

        void onTrack(ShipInfo ship, bool isInRegion)
        {
            FenceTrackMgr.Instance.UpdateTrack(this, ship, isInRegion);
            //TrackChanged?.Invoke(this,ship, isInRegion);
        }
    }
}
