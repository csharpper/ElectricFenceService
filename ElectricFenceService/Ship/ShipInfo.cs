using System;

namespace ElectricFenceService
{
    public class ShipInfo
    {
        public string ID { get; set; }
        public int MMSI { get; set; }

        public string Name { get; set; }
        public int ShipCargoType { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public string CallSign { get; set; }
        public int IMO_Number { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double SOG { get; set; }
        public double COG { get; set; }
        public double TrueHeading { get; set; }
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string ToFormat()
        {
            string str = UpdateTime.ToString("yyyyMMddHHmmssfff");
            str += "," + (MMSI == 0 ? ID : MMSI.ToString());
            str += "," + Name;
            str += "," + ShipCargoType;
            str += "," + CallSign;
            str += "," + IMO_Number;
            str += "," + Length;
            str += "," + Width;
            str += "," + Math.Round(Longitude,7);
            str += "," + Math.Round(Latitude, 7);
            str += "," + Math.Round(SOG,1);
            str += "," + Math.Round(COG,1);
            str += "," + TrueHeading;
            return str;
        }
    }
}