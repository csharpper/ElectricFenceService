using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class ShieldData
    {
        public List<ShieldInfo> Ships { get; set; } = new List<ShieldInfo>();
        public List<ShieldInfo> Types { get; set; } = new List<ShieldInfo>();
        public DateTime UpdatedTime { get; set; } = DateTime.MinValue;

        public ShieldData Clone()
        {
            var res = new ShieldData()
            {
                UpdatedTime = UpdatedTime,
            };
            res.Ships = Ships.Select(_ => _.Clone()).ToList();
            res.Types = Types.Select(_ => _.Clone()).ToList();
            return res;
        }

        public bool IsShield(ShipInfo ship)
        {
            lock (Types)
                if (Types.Any(_ => _.ID == ship.ShipCargoType))
                    return true;
            lock(Ships)
                if (Ships.Any(_ => _.ID == ship.MMSI))
                    return true;
            return false;
        }

        public static string GetShipType(int type)
        {
            if (type >= 20 & type < 30)
                return "地效应船";
            if (type >= 40 & type < 50)
                return "高速船（HSC）";
            if (type >= 60 & type < 70)
                return "客船";
            if (type >= 75 & type < 80)
                return "货船";
            if (type >= 85 & type < 90)
                return "油轮";
            if (type >= 95 & type < 100)
                return "其他";
            switch (type)
            {
                case 30:
                    return "渔船";
                case 31:
                    return "拖轮";
                case 32:
                    return "牵引船";
                case 33:
                    return "水下作业";
                case 34:
                    return "潜水作业";
                case 35:
                    return "军舰";
                case 36:
                    return "航海";
                case 37:
                    return "游艇";
                case 50:
                    return "引航";
                case 51:
                    return "搜救船";
                case 52:
                    return "拖船";
                case 53:
                    return "港口供应船";
                case 54:
                    return "有防污染设施或设备的船舶";
                case 55:
                    return "执法船";
                case 58:
                    return "医疗运输船";
                case 59:
                    return "非战斗舰";
                case 70:
                    return "货船";
                case 71:
                    return "危险品船（A类）";
                case 72:
                    return "危险品船（B类）";
                case 73:
                    return "危险品船（C类）";
                case 74:
                    return "危险品船（D类）";
                case 80:
                    return "油轮";
                case 81:
                    return "油轮（A类）";
                case 82:
                    return "油轮（B类）";
                case 83:
                    return "油轮（C类）";
                case 84:
                    return "油轮（D类）";

                case 90:
                    return "其他";
                case 91:
                    return "其他（A类）";
                case 92:
                    return "其他（B类）";
                case 93:
                    return "其他（C类）";
                case 94:
                    return "其他（D类）";
            }
            return "未知";
        }

        public static byte GetLevel(int type)
        {
            if (type >= 80 && type < 90)//油轮
                return 15;
            if (type >= 71 && type < 75)//危险品船
                return 14;
            if (type >= 70 && type < 80)//货船
                return 10;
            if (type >= 60 && type < 70)//客船
                return 7;
            if (type >= 31 && type < 33)
                return 1;
            if (type >= 50 && type < 56)
                return 1;
            return 10;
        }
    }
}
