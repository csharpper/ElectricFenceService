using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    /// <summary>
    /// ID表示闸机的ID
    /// Links表示关联的区域
    /// </summary>
    public class Bridge: TargetObj
    {
        public List<string> Links { get; set; } = new List<string>();
    }
}
