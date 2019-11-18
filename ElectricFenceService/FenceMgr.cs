using Fence.Util;
using Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElectricFenceService
{
    /// <summary>
    /// 电子围栏区域设置，包含闸机、区域，关联项
    /// </summary>
    public class FenceMgr
    {
        public readonly static FenceMgr Instance = new FenceMgr();
        FenceData _fence;
        string _path = "fence1.json";
        FenceMgr()
        {
            loadFenceData();
        }

        public void Init()
        { }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(_fence, Formatting.Indented);
        }

        private void loadFenceData()
        {
            //if (File.Exists(_path))
            //{
            //    string info = File.ReadAllText(_path);
            //    _fence = JsonConvert.DeserializeObject<FenceData>(info);
            //}
            //else
                newAddSave();
        }

        private void newAddSave()
        {
            _fence = new FenceData()
            {
                Gates = new List<GateInfo>() {
                     new GateInfo() { ID = "00000000354016000017", Name = "兴中1#码头01", Longitude= 122.1373222, Latitude = 29.94655278, Comment="闸机测试" },
                     new GateInfo() { ID = "00000000354016000018", Name = "兴中2#码头01", Longitude= 122.1347333, Latitude = 29.94816111, Comment="闸机测试2"  },
                     new GateInfo() { ID = "00000000354016000019", Name = "兴中4#码头01", Longitude= 122.128825, Latitude = 29.95363, Comment="闸机测试3"  },
                     new GateInfo() { ID = "00000000354016000020", Name = "兴中5#码头01", Longitude= 122.1465972, Latitude = 29.94745556 },
                     new GateInfo() { ID = "00000000354016000021", Name = "兴中5#码头中02", Longitude= 122.1420722, Latitude = 29.94718333 },
                     new GateInfo() { ID = "00000000354016000022", Name = "兴中3号码头", Longitude= 122.133236, Latitude = 29.950094 },
                     //new GateInfo() { ID = "00000000354016000023", Name = "实华2号码头1#", Longitude= 121.945553, Latitude = 30.071627 },
                     //new GateInfo() { ID = "00000000354016000024", Name = "实华2号码头2#", Longitude= 121.942673, Latitude = 30.0681201 },
                     //new GateInfo() { ID = "00000000354016000025", Name = "实华1号码头及全景", Longitude= 121.94475, Latitude = 30.07276667 },
                     //new GateInfo() { ID = "00000000354016000050", Name = "外钓油品码头02云台", Longitude= 121.958858, Latitude = 30.061207 },
                     //new GateInfo() { ID = "00000000354016000051", Name = "外钓油品码头01云台", Longitude= 121.960483, Latitude = 30.0648098 },
                     //new GateInfo() { ID = "00000000354016000052", Name = "外钓油品全景云台", Longitude= 121.963317, Latitude = 30.06325 },
                 },
                Regions = new List<FenceRegionsInfo>() {
                     new FenceRegionsInfo(){ ID = "1", Name = "兴中1号码头", Region=new PointD[]{ new PointD(122.1367981, 29.94607611),new PointD(122.1370567, 29.93903937),new PointD(122.1422549, 29.93901696),new PointD(122.1418928, 29.94746547)}, Comment="区域测试1"},
                     new FenceRegionsInfo(){ ID = "2", Name = "兴中1号码头(内部)", Region=new PointD[]{ new PointD(122.1378237, 29.94636061),new PointD(122.1380353, 29.94501603),new PointD(122.1413269, 29.94564758),new PointD(122.1411153, 29.94719587)}, Comment="区域测试2" },
                     new FenceRegionsInfo(){ ID = "3", Name = "兴中2号码头", Region=new PointD[]{ new PointD(122.1348326, 29.94813773),new PointD(122.1296085, 29.9455607),new PointD(122.1281861, 29.94782401),new PointD(122.1331774, 29.95022173)}, Comment="区域测试3"},
                     new FenceRegionsInfo(){ ID = "4", Name = "兴中2号码头（内部）", Region=new PointD[]{ new PointD(122.133192 , 29.94988496),new PointD(122.1323221, 29.94941641),new PointD(122.1333095, 29.94809224),new PointD(122.1343675, 29.94858117)}, Comment="区域测试4"},
                     new FenceRegionsInfo(){ ID = "5", Name = "兴中3号码头", Region=new PointD[]{ new PointD(122.1328671, 29.95019932),new PointD(122.1280309, 29.94789124),new PointD(122.1265568, 29.95026654),new PointD(122.1307464, 29.95194715)}},
                     new FenceRegionsInfo(){ ID = "6", Name = "兴中3号码头(内部)", Region=new PointD[]{ new PointD(122.1308409, 29.9517795),new PointD(122.1299709, 29.95124985),new PointD(122.1313346, 29.95008868),new PointD(122.1325807, 29.95053685)} },
                     new FenceRegionsInfo(){ ID = "7", Name = "兴中4号码头", Region=new PointD[]{ new PointD(122.1285223, 29.95486013),new PointD(122.1242809, 29.95331402),new PointD(122.1264792, 29.95031136),new PointD(122.1306688, 29.95203678)}},
                     new FenceRegionsInfo(){ ID = "8", Name = "兴中4号码头(内部)", Region=new PointD[]{ new PointD(122.1287719, 29.95446846),new PointD(122.1276433, 29.9538166),new PointD(122.1287719, 29.95210544),new PointD(122.1300415, 29.95265546)} },
                     new FenceRegionsInfo(){ ID = "9", Name = "兴中5号码头", Region=new PointD[]{ new PointD(122.1419704, 29.94742065),new PointD(122.1423584, 29.9391066),new PointD(122.1481514, 29.93917383),new PointD(122.1477376, 29.94802569)}},
                     new FenceRegionsInfo(){ ID = "10", Name = "兴中5号码头(内部)", Region=new PointD[]{ new PointD(122.142573 , 29.94739959),new PointD(122.1428551, 29.9454846),new PointD(122.1468285, 29.94564758),new PointD(122.1467815, 29.94786815)} },
                     //new FenceRegionsInfo(){ ID = "6", Name = "实华1号码头", Outer=new PointD[]{ new PointD(121.9434448, 30.07424112),new PointD(121.9534827, 30.07063668),new PointD(121.9583142, 30.07935919),new PointD(121.9472351, 30.08217042)},Inner=new PointD[]{ new PointD(121.9472767, 30.08029628),new PointD(121.944861 , 30.07607933),new PointD(121.9474433, 30.07535846),new PointD(121.9497758, 30.07939523)} },
                     //new FenceRegionsInfo(){ ID = "7", Name = "实华2号码头", Outer=new PointD[]{ new PointD(121.9439863, 30.07355628),new PointD(121.9547739, 30.06995182),new PointD(121.9502756, 30.06075984),new PointD(121.9381968, 30.0660949 )},Inner=new PointD[]{ new PointD(121.9443195, 30.07189824),new PointD(121.9416538, 30.06814954),new PointD(121.9445694, 30.0671042),new PointD(121.9474433, 30.07060063)} },
                     //new FenceRegionsInfo(){ ID = "8", Name = "外钓油品码头",Outer=new PointD[]{ new PointD(121.9652907, 30.06987973),new PointD(121.9551071, 30.07344815),new PointD(121.9475683, 30.0583806),new PointD(121.9569814, 30.05448718)},Inner=new PointD[]{ new PointD(121.9581476, 30.06447278),new PointD(121.9562108, 30.06003887),new PointD(121.9597928, 30.05913764),new PointD(121.9616671, 30.06367974)} },
                 },
                Bridges = new List<Bridge>() {
                     new Bridge(){ ID = "00000000354016000017", Links = new List<string>(){ "1", "2" } },
                     new Bridge(){ ID = "00000000354016000018", Links = new List<string>(){ "3", "4" } },
                     new Bridge(){ ID = "00000000354016000022", Links = new List<string>(){ "5", "6" } },
                     new Bridge(){ ID = "00000000354016000019", Links = new List<string>(){ "7", "8" } },
                     new Bridge(){ ID = "00000000354016000020", Links = new List<string>(){ "9", "10" } },
                     new Bridge(){ ID = "00000000354016000021", Links = new List<string>(){ "9", "10" } },
                 }
            };
            save();
        }

        public void Set<T>(T region) where T : TargetObj
        {
            _fence.AddOrUpdate(region);
        }

        public void Set<T>(Stream stream) where T : TargetObj
        {
            var body = new StreamReader(stream).ReadToEnd();
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidCastException("请输入有效的数据.");
            var region = JsonConvert.DeserializeObject<T>(body);
            Set(region);
        }

        private void save()
        {
            File.WriteAllText(_path, ToJson());
        }

        public void Delete(List<string> gates, List<string> regions)
        {
            _fence.Delete(gates, regions);
        }
    }
}
