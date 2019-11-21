﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fence.Util
{
    public class FenceData
    {
        /// <summary>闸机列表信息</summary>
        public List<GateInfo> Gates { get; set; } = new List<GateInfo>();
        /// <summary>围栏区域列表信息</summary>
        public List<FenceRegionsInfo> Regions { get; set; } = new List<FenceRegionsInfo>();
        /// <summary>关联列表信息</summary>
        public List<Bridge> Bridges { get; set; } = new List<Bridge>();

        public DateTime UpdateTime { get; set; } = DateTime.MinValue;

        void initIfInvalid()
        {
            if (Bridges == null)
                Bridges = new List<Bridge>();
            if (Gates == null)
                Gates = new List<GateInfo>();
            if (Regions == null)
                Regions = new List<FenceRegionsInfo>();
        }

        public void Delete(List<string> gates, List<string> regions)
        {
            if ((gates == null ||gates.Count == 0) && (regions == null || regions.Count == 0))
                return;
            if (regions != null && gates == null)
            {
                foreach (var id in regions)
                    RemoveRegion(id);
            }
            else if (gates != null && regions == null)
            {
                foreach (var id in gates)
                    RemoveGate(id);
            }
            else
            {
                foreach (var id in gates)
                    foreach (var regId in regions)
                    {
                        RemoveBridge(id, regId);
                    }
            }
        }

        private void delete<T>(List<T> list, List<string> ids) where T : TargetObj
        {
            foreach (var id in ids)
                list.RemoveAll(_ => _.ID == id);
        }

        #region 新增或更新数据
        public void AddOrUpdate<T>(T t) where T : TargetObj
        {
            if (t == null)
                return;
            initIfInvalid();
            if (t is Bridge)
                addOrUpdate(Bridges, t as Bridge);
            if (t is GateInfo)
                addOrUpdate(Gates, t as GateInfo);
            if(t is FenceRegionsInfo)
                addOrUpdate(Regions, t as FenceRegionsInfo);
        }

        private void addOrUpdate<T>(List<T> list, T t) where T: TargetObj
        {
            if (!string.IsNullOrWhiteSpace(t.ID))
            {
                var last = list.FirstOrDefault(_ => _.ID == t.ID);
                if (last != null)
                    t.CreateTime = last.CreateTime;
                list.RemoveAll(_ => _.ID == t.ID);
            }
            else
                t.ID = getNewRegionID();
            list.Add(t);
        }

        private string getNewRegionID()
        {
            for (int i = 1; i < 1000; i++)
            {
                string str = i.ToString("000");
                if (Regions.All(_ => _.ID != str))
                    return str;
            }
            throw new InvalidCastException("添加区域失败，当前区域数量超过上限。");
        }

        #endregion 新增或更新数据

        #region 删除数据
        public void RemoveRegion(string id)
        {
            Regions?.RemoveAll(_ => _.ID == id);
            Bridges?.ForEach(_ => _.Links?.RemoveAll(r => r == id));
            Bridges?.RemoveAll(_ => _.Links == null || _.Links.Count == 0);
        }

        public void RemoveGate(string id)
        {
            Gates?.RemoveAll(_ => _.ID == id);
            Bridges?.RemoveAll(_ => _.ID == id);
        }

        public void RemoveBridge(string id, string regId)
        {
            Bridges?.Where(b=>b.ID == id).ToList().ForEach(_ => _.Links?.RemoveAll(r => r == regId));
            Bridges?.RemoveAll(_ => _.Links == null || _.Links.Count == 0);//删除空的关联列表
        }
        #endregion 删除数据

        public FenceRegionsInfo[] GetBridgeFromGate(string id)
        {
            var regions = Bridges?.FirstOrDefault(_ => _.ID == id);
            List<FenceRegionsInfo> list = new List<FenceRegionsInfo>();
            if (regions != null && regions.Links != null)
            {
                foreach (var r in regions.Links)
                {
                    var reg = Regions?.FirstOrDefault(_ => _.ID == r);
                    if (reg != null)
                        list.Add(reg);
                }
            }
            return list.ToArray();
        }

        public GateInfo[] GetBridgeFromRegion(string id)
        {
            List<GateInfo> list = new List<GateInfo>();
            var regions = Bridges?.Where(_ =>_.Links.Any(l=> l == id)).Select(_=>_.ID);
            if (Gates != null && regions != null && regions.Count() > 0)
            {
                foreach (var r in regions)
                {
                    var reg = Gates.FirstOrDefault(_ => _.ID == r);
                    if (reg != null)
                        list.Add(reg);
                }
            }
            return list.ToArray();
        }
    }
}
