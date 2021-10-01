using Fence.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class GateTrack
    {
        public string objName;//":"2号码头",
        public string shipName;//":"XIECHENG1","
        public string shipMMSI;//":"414400970","
        public string shipGeoLon;//":"1219452000","
        public string shipGeoLat;//":"300725033","
        public string shipSpeed;//":"0","
        public string shipCourse;//":"360","
        public string shipState;//":"","
        public string shipType;//":"防污染设备","
        public string shipWidth;//":"10","
        public string shipLength;//":"49","
        public string shipDraftDepth;//":"0"船舶吃水
        public string shipIMO;//":"0","
        public string shipCallSign;//":"","
        public string reportTime;//":"2019/12/22 16:47:38","
        public string msgType;//":"",1内部区域标记，1进入，2离开
        public string shipDistance;//":"545","
        public string turnStileCode;//":"00000000354016000024"}
        public string objInner;
        public string shipNation;

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static GateTrack Add(ShipInfo ship, GateInfo gate, bool isInner = false, string msgType = "")
        {
            return new GateTrack()
            {
                objName = gate.Name,
                turnStileCode = gate.ID,
                shipMMSI = ship.MMSI.ToString(),
                shipName = ship.Name,
                shipGeoLon = Math.Round(ship.Longitude * 10000000).ToString(),
                shipGeoLat = Math.Round(ship.Latitude * 10000000).ToString(),
                shipSpeed = Math.Round(ship.SOG, 1).ToString(),
                shipCourse = Math.Round(ship.COG).ToString(),
                shipState = "",
                shipIMO = ship.IMO_Number.ToString(),
                shipCallSign = ship.CallSign,
                shipType = ShieldData.GetShipType(ship.ShipCargoType),
                msgType = msgType,
                shipLength = ship.Length.ToString(),
                shipWidth = ship.Width.ToString(),
                shipDraftDepth = "0",
                reportTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                shipDistance = Math.Round(getDis(ship.Longitude, ship.Latitude, gate.Longitude, gate.Latitude)).ToString(),
                objInner = (isInner?"1":"0"),
                shipNation = NationMgr.GetNationFromMMSI(ship.MMSI)
        };
        }

        private static double getDis(double longitude1, double latitude1, double longitude2, double latitude2)
        {
            double dislon = (longitude1 - longitude2) * Math.Cos(latitude2 * Math.PI / 180);
            double disLat = latitude1 - latitude2;
            return Math.Sqrt(dislon * dislon + disLat * disLat) * 60 * 1852;
        }
    }
}
