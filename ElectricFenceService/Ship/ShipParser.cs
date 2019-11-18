using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Ship
{
    public class ShipParser
    {
        object obj = new object();
        public string buffer = "";
        public Action<string> ShipEvent { get; set; }
        public void Add(string info)
        {
            Common.Log.Logger.Default.Trace("received all : " + info);
            lock (obj)
            {
                buffer += info;
                while (buffer.Length > 0)
                {
                    if (!parse())
                        break;
                }
            }
        }

        bool parse()
        {
            int index = buffer.IndexOf('{');
            if (index < 0)
            {
                Common.Log.Logger.Default.Error("not have { : " + buffer);
                buffer = "";
                return false;
            }
            if (index > 0)
                buffer = buffer.Remove(0, index);
            index = buffer.IndexOf('}');
            if (index <= 0)
            {
                Common.Log.Logger.Default.Error("not stop by } : " + buffer);
                return false;
            }
            int length = index + 1;
            ShipEvent?.Invoke(buffer.Substring(0, length));
            buffer = buffer.Remove(0, length);
            return true;
        }
    }
}
