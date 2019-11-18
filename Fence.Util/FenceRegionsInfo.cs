using Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class FenceRegionsInfo: TargetObj
    {
        public string Name { get; set; }
        public PointD[] Region { get; set; }
        public string Comment { get; set; }
    }
}
