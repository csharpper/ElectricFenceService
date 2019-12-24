using Fence.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Fence
{
    public class FenceCensorMgr
    {
        public static FenceCensorMgr Instance { get; private set; } = new FenceCensorMgr();

        public FenceData Data { get; private set; }
        DateTime _updatedTime = DateTime.MinValue;
        public string _path;
        object _obj = new object();
        FenceCensorMgr()
        {
            _path = ConfigData.FenceFileName;
            Data = new FenceData();
            new Thread(run) { IsBackground = true }.Start();
        }

        private void run()
        {
            while (true)
            {
                try
                {///实时刷新
                    FileInfo fi = new FileInfo(_path);
                    if (fi.Exists)
                    {
                        if (fi.LastWriteTime > _updatedTime)
                        {
                            string info = File.ReadAllText(_path);
                            lock(_obj)
                                Data = JsonConvert.DeserializeObject<FenceData>(info);
                            _updatedTime = fi.LastWriteTime;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.Log.Logger.Default.Error(ex.Message);
                }
                Thread.Sleep(1000);
            }
        }

        public void Init()
        { }
    }
}
