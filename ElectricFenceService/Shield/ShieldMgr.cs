using Fence.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.Shield
{
    public class ShieldMgr
    {
        public readonly static ShieldMgr Instance = new ShieldMgr();
        ShieldData _datas;
        public Action ShieldChanged { get; set; }
        string _path;
        ShieldMgr()
        {
            _path = ConfigData.ShieldFileName;
            loadData();
        }

        public void Init()
        { }

        public ShieldData GetShield()
        {
            return _datas.Clone();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(_datas, Formatting.Indented);
        }

        private void loadData()
        {
            if (File.Exists(_path))
            {
                string info = File.ReadAllText(_path);
                _datas = JsonConvert.DeserializeObject<ShieldData>(info);
            }
            else
                newAddSave();
        }


        private void newAddSave()
        {
            _datas = new ShieldData()
            {
                Types = new List<ShieldInfo>()
                {
                    new ShieldInfo() { ID= 50, Name="引航" },
                    new ShieldInfo() { ID= 52, Name="拖船" }
                }
            };
            save();
        }

        public void SetFromMMSI(IEnumerable<int> infos)
        {
            lock (_datas)
            {
                foreach (var info in infos)
                {
                    string name = null;// ShieldData.GetShipType(info);
                    set(_datas.Ships, new ShieldInfo() { ID =info, Name = name });
                }
            }
        }

        public void SetFromType(IEnumerable<int> infos)
        {
            lock (_datas)
            {
                foreach (var info in infos)
                {
                    string name = ShieldData.GetShipType(info);//更新船舶类型名称
                    set(_datas.Types, new ShieldInfo() { ID = info, Name = name } );
                }
            }
        }

        public void RemoveFromMMSI(IEnumerable<int> mmsis)
        {
            lock (_datas)
            {
                foreach (var mmsi in mmsis)
                    _datas.Ships.RemoveAll(_ => _.ID == mmsi);
                onUpdated();
            }
        }

        public void RemoveFromTypes(IEnumerable<int> types)
        {
            lock (_datas)
            {
                foreach (var type in types)
                    _datas.Types.RemoveAll(_ => _.ID == type);
                onUpdated();
            }
        }

        void set(List<ShieldInfo> datas, ShieldInfo newData)
        {
            lock (_datas)
            {
                if (datas.All(_ => _.ID != newData.ID))
                {
                    datas.Add(newData);
                    onUpdated();
                }
            }
        }

        void onUpdated()
        {
            _datas.UpdatedTime = DateTime.Now;
            save();
        }

        private void save()
        {
            File.WriteAllText(_path, ToJson());
        }
    }
}
