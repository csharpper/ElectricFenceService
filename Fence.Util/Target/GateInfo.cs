using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class GateInfo: TargetObj
    {
        public string Name { get; set; }
        public int Priority { get; set; } = 1;//优先级，数字越小，优先级越高
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Comment { get; set; }
    }
}
