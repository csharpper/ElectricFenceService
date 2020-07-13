using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Util
{
    public static class ConfigDataMgr
    {
        /// <summary>船舶跟踪超时时间</summary>
        public static int TrackTimeout { get; set; } = 180;
        /// <summary>船舶信号超时时间</summary>
        public static int SignalTimeout { get; set; } = 15 * 60;
        /// <summary>雷达信号超时时间</summary>
        public static double RadarTimeout { get; set; } = 15;
    }
}
