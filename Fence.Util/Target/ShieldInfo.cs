using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class ShieldInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ShieldInfo Clone()
        {
            return (ShieldInfo)MemberwiseClone();
        }
    }
}
