using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fence.Util
{
    public class MuxReceiver : DataReceiver
    {
        public MuxReceiver(string host, int port)
            : base(host, port)
        {
        }

        Parser _parse = new Parser();
        protected override void received(byte[] buffer)
        {
            string info = Encoding.Default.GetString(buffer);
            string[] ships = _parse.Parse(info);
            //string[] ships = info.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ship in ships)
            {
                string[] settings = ship.Split(',');
                int index = 0;
                try
                {
                    if (settings[index++].Equals("track") && settings.Length >= 13)
                    {
                        string id = settings[index++];
                        int mmsi = int.Parse(settings[index++]);
                        int imo = int.Parse(settings[index++]);
                        string name = settings[index++];
                        int type = 0;
                        int.TryParse(settings[index++], out type);
                        double lat = double.Parse(settings[index++]);
                        double lon = double.Parse(settings[index++]);
                        double sog = double.Parse(settings[index++]);
                        double cog = double.Parse(settings[index++]);
                        double length = double.Parse(settings[index++]);
                        double width = double.Parse(settings[index++]);
                        double heading = 511;
                        if (settings.Length > index)
                            double.TryParse(settings[index++], out heading);
                        string callsign = null;
                        if (settings.Length > index)
                        {
                            callsign = settings[index++];
                        }
                        ShipInfo target = new ShipInfo()
                        {
                            ID = id,
                            MMSI = mmsi,
                            IMO_Number = imo,
                            Name = name,
                            ShipCargoType = type,
                            Latitude = lat,
                            Longitude = lon,
                            SOG = sog,
                            COG = cog,
                            Length = length,
                            Width = width,
                            CallSign = callsign,
                            TrueHeading = heading,
                        };
                        onDynamic(target);
                    }
                    else
                        Common.Log.Logger.Default.Error($"Received Error Data: {ship}");
                }
                catch (Exception ex)
                {
                    Common.Log.Logger.Default.Error($"Received Error Data: {ship} - " + ex);
                }
            }
        }
    }
}
