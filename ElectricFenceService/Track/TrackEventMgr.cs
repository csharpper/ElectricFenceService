using Fence.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Track
{
    public class TrackEventMgr
    {
        public static TrackEventMgr Instance { get; } = new TrackEventMgr();
        /// <summary>记录每个区域内经过的船舶实时数据，含缓存时间</summary>
        Queue<string> _tracks = new Queue<string>();

        public void Enqueue(string shipId)
        {
            lock (_tracks)
            {
                if (_tracks.All(_ => _ != shipId))
                    _tracks.Enqueue(shipId);
            }
        }

        public string Dequeue()
        {
            lock (_tracks)
            {
                if (_tracks.Count > 0)
                    return _tracks.Dequeue();
                return null;
            }
        }
        
    }

}
