using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class ShipUnion
    {
        public string ID { get; set; }
        public int MMSI { get; set; }
        public string Name { get; set; }
        public string IMO { get; set; }
        public int CellSign { get; set; }
        public int Type { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double SOG { get; set; }
        public double COG { get; set; }
        public double Heading { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
    }
}
